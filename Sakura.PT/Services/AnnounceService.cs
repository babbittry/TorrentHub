using BencodeNET;
using BencodeNET.Objects;
using BencodeNET.Torrents;
using Microsoft.EntityFrameworkCore;
using Sakura.PT.Data;
using Sakura.PT.Entities;
using System.Web;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Options;
using Sakura.PT.Enums;

namespace Sakura.PT.Services;

public class AnnounceService : IAnnounceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnnounceService> _logger;
    private readonly SakuraCoinSettings _sakuraCoinSettings;

    public AnnounceService(ApplicationDbContext context, ILogger<AnnounceService> logger, IOptions<SakuraCoinSettings> sakuraCoinSettings)
    {
        _context = context;
        _logger = logger;
        _sakuraCoinSettings = sakuraCoinSettings.Value;
    }

    public async Task<BDictionary> ProcessAnnounceRequest(
        string infoHash,
        string peerId,
        int port,
        long uploaded,
        long downloaded,
        long left,
        string? @event,
        int numWant,
        string? key,
        string? ipAddress,
        string passkey)
    {
        _logger.LogInformation("Processing announce request for infoHash: {InfoHash}, peerId: {PeerId}, event: {Event}", infoHash, peerId, @event);

        // Authenticate user by passkey
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Passkey == passkey);
        if (user == null)
        {
            _logger.LogWarning("Announce request with invalid passkey: {Passkey}", passkey);
            throw new UnauthorizedAccessException("Invalid passkey.");
        }

        // Convert infoHash from URL-encoded to byte array (assuming it's 20 bytes SHA1)
        var infoHashBytes = HttpUtility.UrlDecodeToBytes(infoHash);
        if (infoHashBytes == null || infoHashBytes.Length != 20)
        {
            _logger.LogWarning("Announce request with invalid info_hash format: {InfoHash}", infoHash);
            throw new ArgumentException("Invalid info_hash.");
        }
        var infoHashString = BitConverter.ToString(infoHashBytes).Replace("-", "").ToLowerInvariant();


        var torrent = await _context.Torrents.FirstOrDefaultAsync(t => t.InfoHash == infoHashString);
        if (torrent == null)
        {
            _logger.LogWarning("Announce request for non-existent torrent: {InfoHashString}", infoHashString);
            throw new KeyNotFoundException("Torrent not found.");
        }

        if (string.IsNullOrEmpty(ipAddress))
        {
            _logger.LogError("Could not determine client IP address for announce request.");
            throw new ArgumentException("Could not determine client IP address.");
        }

        // Handle peer events (started, completed, stopped, none)
        var peer = await _context.Peers.FirstOrDefaultAsync(p => p.TorrentId == torrent.Id && p.UserId == user.Id);

        // Calculate seeding time for existing peers
        if (peer != null && peer.IsSeeder) // Only accumulate time if they were seeding
        {
            var timeSeeding = (DateTime.UtcNow - peer.LastAnnounce).TotalMinutes;
            if (timeSeeding > 0 && timeSeeding <= 3600) // Cap at 1 hour to prevent abuse from long gaps
            {
                user.TotalSeedingTimeMinutes += (long)timeSeeding;
                _logger.LogDebug("User {UserId} accumulated {Time} minutes of seeding time. Total: {Total}", user.Id, timeSeeding, user.TotalSeedingTimeMinutes);
            }
        }

        switch (@event)
        {
            case "started":
            case null: // No event means "started" or "none"
                if (peer == null)
                {
                    _logger.LogInformation("Peer {PeerId} started for torrent {TorrentId} (User: {UserId}).", peerId, torrent.Id, user.Id);
                    peer = new Peers
                    {
                        TorrentId = torrent.Id,
                        Torrent = torrent,
                        UserId = user.Id,
                        User = user,
                        IpAddress = ipAddress,
                        Port = port,
                        LastAnnounce = DateTime.UtcNow,
                        IsSeeder = (left == 0)
                    };
                    _context.Peers.Add(peer);
                }
                else
                {
                    _logger.LogInformation("Peer {PeerId} re-announced for torrent {TorrentId} (User: {UserId}).", peerId, torrent.Id, user.Id);
                    peer.IpAddress = ipAddress;
                    peer.Port = port;
                    peer.LastAnnounce = DateTime.UtcNow;
                    peer.IsSeeder = (left == 0);
                }
                break;
            case "completed":
                _logger.LogInformation("Peer {PeerId} completed torrent {TorrentId} (User: {UserId}).", peerId, torrent.Id, user.Id);
                if (peer != null)
                {
                    peer.LastAnnounce = DateTime.UtcNow;
                    peer.IsSeeder = true; // Peer completed, so it's now a seeder
                }
                break;
            case "stopped":
                _logger.LogInformation("Peer {PeerId} stopped for torrent {TorrentId} (User: {UserId}).", peerId, torrent.Id, user.Id);
                if (peer != null)
                {
                    _context.Peers.Remove(peer);
                }
                break;
        }

        // Update user's uploaded and downloaded bytes
        long actualUploaded = uploaded;
        if (user.IsDoubleUploadActive && user.DoubleUploadExpiresAt > DateTime.UtcNow)
        {
            actualUploaded *= 2;
            _logger.LogInformation("User {UserId} has double upload active. Uploaded bytes doubled to {ActualUploaded}.", user.Id, actualUploaded);
        }
        else if (user.IsDoubleUploadActive && user.DoubleUploadExpiresAt <= DateTime.UtcNow)
        {
            // Double upload expired, reset status
            user.IsDoubleUploadActive = false;
            user.DoubleUploadExpiresAt = null;
            _logger.LogInformation("User {UserId} double upload expired and reset.", user.Id);
        }

        user.UploadedBytes += actualUploaded;
        user.DownloadedBytes += downloaded;
        _logger.LogInformation("User {UserId} stats updated: Uploaded {Uploaded}, Downloaded {Downloaded}.", user.Id, user.UploadedBytes, user.DownloadedBytes);

        // Check and reset No H&R status if expired
        if (user.IsNoHRActive && user.NoHRExpiresAt <= DateTime.UtcNow)
        {
            user.IsNoHRActive = false;
            user.NoHRExpiresAt = null;
            _logger.LogInformation("User {UserId} No H&R status expired and reset.", user.Id);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Announce request processed successfully for infoHash: {InfoHash}, peerId: {PeerId}.", infoHash, peerId);

        // Prepare response
        var response = new BDictionary
        {
            { "interval", new BNumber(1800) }, // Announce interval in seconds
            { "min interval", new BNumber(900) } // Minimum announce interval
        };

        // Get peers for response
        var peers = await _context.Peers
            .Where(p => p.TorrentId == torrent.Id && p.UserId != user.Id) // Exclude current peer
            .Take(numWant)
            .ToListAsync();

        var peerList = new BList();
        foreach (var p in peers)
        {
            var peerDict = new BDictionary
            {
                { "peer id", new BString(p.UserId.ToString()) }, // Using UserId as peer id for now
                { "ip", new BString(p.IpAddress) },
                { "port", new BNumber(p.Port) }
            };
            peerList.Add(peerDict);
        }
        response.Add("peers", peerList);

        return response;
    }

    /// <summary>
    /// Gets the H&R exemption hours for a given user role.
    /// </summary>
    /// <param name="role">The user's role.</param>
    /// <returns>The number of hours the user is exempt from H&R rules.</returns>
    private int GetHRExemptionHours(UserRole role)
    {
        return role switch
        {
            UserRole.User => _sakuraCoinSettings.UserHRExemptionHours,
            UserRole.PowerUser => _sakuraCoinSettings.PowerUserHRExemptionHours,
            UserRole.EliteUser => _sakuraCoinSettings.EliteUserHRExemptionHours,
            UserRole.CrazyUser => _sakuraCoinSettings.CrazyUserHRExemptionHours,
            UserRole.VeteranUser => _sakuraCoinSettings.VeteranUserHRExemptionHours,
            UserRole.VIP => _sakuraCoinSettings.VIPHRExemptionHours,
            _ => 0 // Other roles (Mosquito, Seeder, Staff) have no special H&R exemption by default
        };
    }
}