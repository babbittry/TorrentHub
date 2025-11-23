using BencodeNET.Objects;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Web;
using TorrentHub.Core.Data;
using TorrentHub.Core.Entities;
using TorrentHub.Core.Enums;
using TorrentHub.Core.Services;

namespace TorrentHub.Tracker.Services;

public class AnnounceService : IAnnounceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnnounceService> _logger;
    private readonly ISettingsReader _settingsReader;
    private readonly IAdminService _adminService;
    private readonly ITrackerLocalizer _localizer;
    private readonly ITorrentCredentialService _credentialService;

    public AnnounceService(
        ApplicationDbContext context,
        ILogger<AnnounceService> logger,
        ISettingsReader settingsReader,
        IAdminService adminService,
        ITrackerLocalizer localizer,
        ITorrentCredentialService credentialService)
    {
        _context = context;
        _logger = logger;
        _settingsReader = settingsReader;
        _adminService = adminService;
        _localizer = localizer;
        _credentialService = credentialService;
    }

    public async Task<byte[]> HandleAnnounceAsync(HttpContext context)
    {
        // Extract credential from query string
        var credentialParam = context.Request.Query["credential"].ToString();
        if (string.IsNullOrEmpty(credentialParam) || !Guid.TryParse(credentialParam, out var credential))
        {
            return _localizer.GetError("InvalidCredential", null).EncodeAsBytes();
        }

        // Extract announce parameters
        var infoHash = context.Request.Query["info_hash"].ToString();
        var peerId = context.Request.Query["peer_id"].ToString();
        var portStr = context.Request.Query["port"].ToString();
        var uploadedStr = context.Request.Query["uploaded"].ToString();
        var downloadedStr = context.Request.Query["downloaded"].ToString();
        var leftStr = context.Request.Query["left"].ToString();
        var @event = context.Request.Query["event"].ToString();
        var numWantStr = context.Request.Query["numwant"].ToString();
        
        if (!int.TryParse(portStr, out var port) ||
            !ulong.TryParse(uploadedStr, out var uploaded) ||
            !ulong.TryParse(downloadedStr, out var downloaded) ||
            !long.TryParse(leftStr, out var left))
        {
            return _localizer.GetError("InvalidInfoHash", null).EncodeAsBytes();
        }
        
        var numWant = int.TryParse(numWantStr, out var nw) ? nw : 50;
        var ipAddress = context.Connection.RemoteIpAddress;

        var settings = await _settingsReader.GetSiteSettingsAsync();

        // --- 1. Credential Validation ---
        var (isValid, userId, torrentId) = await _credentialService.ValidateCredentialAsync(credential);
        if (!isValid || !userId.HasValue || !torrentId.HasValue)
        {
            _logger.LogWarning("Announce with invalid credential: {Credential}", credential);
            return _localizer.GetError("InvalidCredential", null).EncodeAsBytes();
        }

        // Update credential usage statistics
        // This call is now redundant as we update usage stats below
        // await _credentialService.UpdateCredentialUsageAsync(credential);

        // --- 2. User and Client Validation ---
        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
        {
            _logger.LogWarning("User not found for credential: {Credential}, userId: {UserId}", credential, userId.Value);
            return _localizer.GetError("InvalidCredential", null).EncodeAsBytes();
        }
        
        if (user.BanStatus.HasFlag(BanStatus.TrackerBan) || user.BanStatus.HasFlag(BanStatus.LoginBan))
        {
            _logger.LogWarning("Announce from banned user: {UserId}", user.Id);
            return _localizer.GetError("BannedAccount", user.Language).EncodeAsBytes();
        }

        var bannedClients = await _adminService.GetBannedClientsAsync();
        if (bannedClients.Any(c => peerId.StartsWith(c.UserAgentPrefix)))
        {
            _logger.LogWarning("Announce from banned client: {PeerId}", peerId);
            return _localizer.GetError("BannedClient", user.Language).EncodeAsBytes();
        }

        // --- 3. Torrent Validation ---
        byte[] infoHashBytes;
        try { infoHashBytes = ParseInfoHash(infoHash); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decode info_hash: {InfoHash}", infoHash);
            return _localizer.GetError("InvalidInfoHash", user.Language).EncodeAsBytes();
        }

        var torrent = await _context.Torrents.FindAsync(torrentId.Value);
        if (torrent == null)
        {
            _logger.LogWarning("Torrent not found for credential: {Credential}, torrentId: {TorrentId}", credential, torrentId.Value);
            return _localizer.GetError("TorrentNotFound", user.Language).EncodeAsBytes();
        }

        // Verify that the info_hash matches the torrent from credential
        if (!torrent.InfoHash.SequenceEqual(infoHashBytes))
        {
            _logger.LogWarning("InfoHash mismatch: credential torrentId {TorrentId} has different infohash than provided {InfoHash}", torrentId.Value, infoHash);
            return _localizer.GetError("TorrentNotFound", user.Language).EncodeAsBytes();
        }

        // --- 4. Peer & Event Processing ---
        var peer = await _context.Peers.FirstOrDefaultAsync(p => p.TorrentId == torrent.Id && p.UserId == user.Id);
        double timeDelta = (peer != null) ? (DateTimeOffset.UtcNow - peer.LastAnnounce).TotalSeconds : 0;

        // --- 4.1. Announce Frequency Check ---
        if (peer != null && timeDelta > 0)
        {
            // Enforce absolute minimum interval
            if (timeDelta < settings.EnforcedMinAnnounceIntervalSeconds)
            {
                var waitTime = settings.EnforcedMinAnnounceIntervalSeconds - (int)timeDelta;
                _logger.LogWarning(
                    "Announce too frequent: User {UserId}, Torrent {TorrentId}, Interval: {Interval}s (min: {Min}s)",
                    user.Id, torrent.Id, timeDelta, settings.EnforcedMinAnnounceIntervalSeconds
                );
                
                var errorDict = new BDictionary
                {
                    { "failure reason", new BString($"Announce too frequent. Please wait {waitTime} seconds.") },
                    { "interval", new BNumber(settings.MinAnnounceIntervalSeconds) },
                    { "min interval", new BNumber(settings.EnforcedMinAnnounceIntervalSeconds) }
                };
                return errorDict.EncodeAsBytes();
            }
        }

        // --- 4.2. Multi-Location Detection ---
        if (settings.EnableMultiLocationDetection && peer != null && ipAddress != null)
        {
            var currentIp = ipAddress.ToString();
            var lastIp = peer.IpAddress?.ToString();
            
            if (currentIp != lastIp)
            {
                // Get recent IPs for this user/torrent combination
                var detectionWindow = DateTimeOffset.UtcNow.AddMinutes(-settings.MultiLocationDetectionWindowMinutes);
                var recentIps = await _context.Peers
                    .Where(p => p.UserId == user.Id && 
                                p.TorrentId == torrent.Id &&
                                p.LastAnnounce >= detectionWindow)
                    .Select(p => p.IpAddress.ToString())
                    .Distinct()
                    .ToListAsync();
                
                if (recentIps.Count >= 3 && settings.LogMultiLocationCheating)
                {
                    await _adminService.LogCheatAsync(
                        userId: user.Id,
                        detectionType: CheatDetectionType.MultiLocation,
                        severity: CheatSeverity.High,
                        details: $"IPs in {settings.MultiLocationDetectionWindowMinutes}min: {string.Join(", ", recentIps)}",
                        torrentId: torrent.Id,
                        ipAddress: currentIp
                    );
                }
            }
        }

        // --- 5. Speed Check ---
        if (peer != null && timeDelta > settings.MinSpeedCheckIntervalSeconds)
        {
            if (uploaded > peer.Uploaded)
            {
                double uploadSpeedKBps = ((uploaded - peer.Uploaded) / 1024.0) / timeDelta;
                if (settings.MaxUploadSpeed > 0 && uploadSpeedKBps > settings.MaxUploadSpeed)
                {
                    var severity = uploadSpeedKBps > (settings.MaxUploadSpeed * 5) ? CheatSeverity.Critical : CheatSeverity.Medium;
                    await _adminService.LogCheatAsync(
                        userId: user.Id,
                        detectionType: CheatDetectionType.SpeedCheat,
                        severity: severity,
                        details: $"Upload speed {uploadSpeedKBps:F2} KB/s exceeds limit",
                        torrentId: torrent.Id,
                        ipAddress: ipAddress?.ToString()
                    );
                    return _localizer.GetError("SpeedTooHigh", user.Language).EncodeAsBytes();
                }
            }
        }

        // Update peer activity times
        if (peer != null && timeDelta > 0 && timeDelta <= 3600)
        {
            if (peer.IsSeeder) user.TotalSeedingTimeMinutes += (ulong)timeDelta / 60;
            else user.TotalLeechingTimeMinutes += (ulong)timeDelta / 60;
        }

        // Handle events
        switch (@event)
        {
            case "started":
            case null:
            case "":
                if (peer == null)
                {
                    peer = new Peers
                    {
                        Torrent = torrent,
                        User = user,
                        IpAddress = ipAddress ?? throw new InvalidOperationException("IP address is required"),
                        Port = port,
                        UserAgent = peerId,
                        Uploaded = uploaded,
                        Downloaded = downloaded,
                        Credential = credential
                    };
                    _context.Peers.Add(peer);
                }
                if (ipAddress != null)
                {
                    peer.IpAddress = ipAddress;
                }
                peer.Port = port;
                peer.LastAnnounce = DateTimeOffset.UtcNow;
                peer.IsSeeder = (left == 0);
                peer.Credential = credential;
                break;
            case "completed":
                if (peer != null && !peer.IsSeeder)
                {
                    torrent.Snatched++;
                    peer.IsSeeder = true;
                }
                break;
            case "stopped":
                if (peer != null) _context.Peers.Remove(peer);
                break;
        }
        
        if (peer != null)
        {
            peer.Uploaded = uploaded;
            peer.Downloaded = downloaded;
        }

        // --- 6. Stats Update ---
        var nominalUpload = uploaded;
        var nominalDownload = downloaded;

        bool isFreeLeech = settings.GlobalFreeleechEnabled || 
                          (torrent.IsFree && (!torrent.FreeUntil.HasValue || torrent.FreeUntil.Value > DateTimeOffset.UtcNow));
        if (isFreeLeech) nominalDownload = 0;

        if (user.IsDoubleUploadActive && user.DoubleUploadExpiresAt > DateTimeOffset.UtcNow)
            nominalUpload *= 2;
        else if (user.IsDoubleUploadActive)
            user.IsDoubleUploadActive = false;

        user.UploadedBytes += uploaded;
        user.DownloadedBytes += downloaded;
        user.NominalUploadedBytes += nominalUpload;
        user.NominalDownloadedBytes += nominalDownload;

        // --- 6.1. Update Credential Usage Statistics ---
        var credentialEntity = await _context.TorrentCredentials
            .FirstOrDefaultAsync(c => c.Credential == credential);

        if (credentialEntity != null)
        {
            // Calculate traffic deltas
            if (peer != null && timeDelta > 0)
            {
                var uploadDelta = uploaded > peer.Uploaded ? uploaded - peer.Uploaded : 0;
                var downloadDelta = downloaded > peer.Downloaded ? downloaded - peer.Downloaded : 0;
                
                credentialEntity.TotalUploadedBytes += uploadDelta;
                credentialEntity.TotalDownloadedBytes += downloadDelta;
            }
            
            // Update announce count and timestamps
            credentialEntity.AnnounceCount++;
            credentialEntity.LastUsedAt = DateTimeOffset.UtcNow;
            
            if (!credentialEntity.FirstUsedAt.HasValue)
            {
                credentialEntity.FirstUsedAt = DateTimeOffset.UtcNow;
            }
            
            // Update IP and UserAgent
            if (ipAddress != null)
            {
                credentialEntity.LastIpAddress = ipAddress.ToString();
            }
            credentialEntity.LastUserAgent = peerId;
        }

        await _context.SaveChangesAsync();

        // --- 7. Prepare Response ---
        var response = new BDictionary
        {
            { "interval", new BNumber(settings.MinAnnounceIntervalSeconds) },
            { "min interval", new BNumber(settings.EnforcedMinAnnounceIntervalSeconds) }
        };

        var peers = await _context.Peers
            .Where(p => p.TorrentId == torrent.Id && p.UserId != user.Id)
            .Take(numWant)
            .ToListAsync();

        response.Add("peers", ToPeerList(peers));
        return response.EncodeAsBytes();
    }

    private static BList ToPeerList(IEnumerable<Peers> peers)
    {
        var peerList = new BList();
        foreach (var p in peers)
        {
            peerList.Add(new BDictionary
            {
                { "peer id", new BString(p.UserAgent) },
                { "ip", new BString(p.IpAddress?.ToString() ?? "") },
                { "port", new BNumber(p.Port) }
            });
        }
        return peerList;
    }

    private static byte[] ParseInfoHash(string infoHash)
    {
        try
        {
            var decodedBytes = HttpUtility.UrlDecodeToBytes(infoHash);
            if (decodedBytes.Length == 20) return decodedBytes;

            var hexString = System.Text.Encoding.UTF8.GetString(decodedBytes);
            if (hexString.Length == 40)
            {
                return Convert.FromHexString(hexString);
            }
        }
        catch { }
        throw new ArgumentException("Invalid info_hash format.");
    }
}
