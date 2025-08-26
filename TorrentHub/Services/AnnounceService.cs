
using BencodeNET.Objects;
using Microsoft.EntityFrameworkCore;
using System.Web;
using TorrentHub.Data;
using TorrentHub.Entities;
using TorrentHub.Enums;

namespace TorrentHub.Services;

public class AnnounceService : IAnnounceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnnounceService> _logger;
    private readonly ISettingsService _settingsService;
    private readonly IAdminService _adminService;

    public AnnounceService(ApplicationDbContext context, ILogger<AnnounceService> logger, ISettingsService settingsService, IAdminService adminService)
    {
        _context = context;
        _logger = logger;
        _settingsService = settingsService;
        _adminService = adminService;
    }

    public async Task<BDictionary> ProcessAnnounceRequest(
        string infoHash,
        string peerId,
        int port,
        ulong uploaded,
        ulong downloaded,
        long left,
        string? @event,
        int numWant,
        System.Net.IPAddress ipAddress,
        Guid passkey)
    {
        var settings = await _settingsService.GetSiteSettingsAsync();

        // --- 1. User and Client Validation ---
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Passkey == passkey);
        if (user == null) { _logger.LogWarning("Announce with invalid passkey: {Passkey}", passkey); return AnnounceError("Invalid passkey."); }
        if (user.BanStatus.HasFlag(BanStatus.TrackerBan) || user.BanStatus.HasFlag(BanStatus.LoginBan)) { _logger.LogWarning("Announce from banned user: {UserId}", user.Id); return AnnounceError("Your account is banned."); }

        var bannedClients = await _adminService.GetBannedClientsAsync();
        if (bannedClients.Any(c => peerId.StartsWith(c.UserAgentPrefix))) { _logger.LogWarning("Announce from banned client: {PeerId}", peerId); return AnnounceError("Your client is banned."); }

        // --- 2. Torrent Validation ---
        byte[] infoHashBytes;
        try { infoHashBytes = ParseInfoHash(infoHash); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to decode info_hash: {InfoHash}", infoHash); return AnnounceError("Invalid info_hash format."); }

        var torrent = await _context.Torrents.FirstOrDefaultAsync(t => t.InfoHash == infoHashBytes);
        if (torrent == null) { _logger.LogWarning("Announce for non-existent torrent: {InfoHash}", infoHash); return AnnounceError("Torrent not found."); }

        // --- 3. Peer & Event Processing ---
        var peer = await _context.Peers.FirstOrDefaultAsync(p => p.TorrentId == torrent.Id && p.UserId == user.Id);
        double timeDelta = (peer != null) ? (DateTimeOffset.UtcNow - peer.LastAnnounce).TotalSeconds : 0;

        // --- 4. Speed Check ---
        if (peer != null && timeDelta > 5) // Only check if interval is reasonable
        {
            if (uploaded > peer.Uploaded)
            {
                double uploadSpeedKBps = ((uploaded - peer.Uploaded) / 1024.0) / timeDelta;
                if (settings.MaxUploadSpeed > 0 && uploadSpeedKBps > settings.MaxUploadSpeed)
                {
                    await _adminService.LogCheatAsync(user.Id, "Speed Cheat (Upload)", $"Reported upload speed {uploadSpeedKBps:F2} KB/s exceeds limit of {settings.MaxUploadSpeed} KB/s.");
                    return AnnounceError("Reported upload speed is too high. This event has been logged.");
                }
            }
        }

        // Update peer activity times
        if (peer != null && timeDelta > 0 && timeDelta <= 3600) // Cap at 1 hour
        {
            if (peer.IsSeeder) user.TotalSeedingTimeMinutes += (ulong)timeDelta / 60;
            else user.TotalLeechingTimeMinutes += (ulong)timeDelta / 60;
        }

        switch (@event)
        {
            case "started":
            case null:
                                if (peer == null)
                {
                    peer = new Peers { Torrent = torrent, User = user, IpAddress = ipAddress, Port = port, UserAgent = peerId, Uploaded = uploaded, Downloaded = downloaded };
                    _context.Peers.Add(peer);
                }
                peer.IpAddress = ipAddress;
                peer.Port = port;
                peer.LastAnnounce = DateTimeOffset.UtcNow;
                peer.IsSeeder = (left == 0);
                break;
            case "completed":
                if (peer != null && !peer.IsSeeder) { torrent.Snatched++; peer.IsSeeder = true; }
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

        // --- 5. Stats Update ---
        var nominalUpload = uploaded;
        var nominalDownload = downloaded;

        bool isFreeLeech = settings.GlobalFreeleechEnabled || (torrent.IsFree && (!torrent.FreeUntil.HasValue || torrent.FreeUntil.Value > DateTimeOffset.UtcNow));
        if (isFreeLeech) nominalDownload = 0;

        if (user.IsDoubleUploadActive && user.DoubleUploadExpiresAt > DateTimeOffset.UtcNow) nominalUpload *= 2;
        else if (user.IsDoubleUploadActive) user.IsDoubleUploadActive = false;

        user.UploadedBytes += uploaded;
        user.DownloadedBytes += downloaded;
        user.NominalUploadedBytes += nominalUpload;
        user.NominalDownloadedBytes += nominalDownload;

        await _context.SaveChangesAsync();

        // --- 6. Prepare Response ---
        var response = new BDictionary
        {
            { "interval", new BNumber(settings.AnnounceIntervalSeconds) },
            { "min interval", new BNumber(settings.AnnounceIntervalSeconds / 2) }
        };

        var peers = await _context.Peers
            .Where(p => p.TorrentId == torrent.Id && p.UserId != user.Id)
            .Take(numWant)
            .ToListAsync();

        response.Add("peers", ToPeerList(peers));
        return response;
    }

    private static BList ToPeerList(IEnumerable<Peers> peers)
    {
        var peerList = new BList();
        foreach (var p in peers)
        {
            peerList.Add(new BDictionary
            {
                { "peer id", new BString(p.UserAgent) },
                { "ip", new BString(p.IpAddress.ToString()) },
                { "port", new BNumber(p.Port) }
            });
        }
        return peerList;
    }

    private static BDictionary AnnounceError(string message)
    {
        return new BDictionary { { "failure reason", new BString(message) } };
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
        catch {}
        throw new ArgumentException("Invalid info_hash format.");
    }
}
