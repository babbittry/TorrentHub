using BencodeNET;
using BencodeNET.Objects;
using BencodeNET.Torrents;
using Microsoft.EntityFrameworkCore;
using Sakura.PT.Data;
using Sakura.PT.Entities;
using System.Web;
using Microsoft.Extensions.Logging;

namespace Sakura.PT.Services;

public class AnnounceService : IAnnounceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnnounceService> _logger;

    public AnnounceService(ApplicationDbContext context, ILogger<AnnounceService> logger)
    {
        _context = context;
        _logger = logger;
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
        string? ipAddress)
    {
        _logger.LogInformation("Processing announce request for infoHash: {InfoHash}, peerId: {PeerId}, event: {Event}", infoHash, peerId, @event);

        // Authenticate user by passkey
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("Announce request missing passkey.");
            throw new ArgumentException("Missing passkey.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Passkey == key);
        if (user == null)
        {
            _logger.LogWarning("Announce request with invalid passkey: {Passkey}", key);
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
                        LastAnnounce = DateTime.UtcNow
                    };
                    _context.Peers.Add(peer);
                }
                else
                {
                    _logger.LogInformation("Peer {PeerId} re-announced for torrent {TorrentId} (User: {UserId}).", peerId, torrent.Id, user.Id);
                    peer.IpAddress = ipAddress;
                    peer.Port = port;
                    peer.LastAnnounce = DateTime.UtcNow;
                }
                break;
            case "completed":
                _logger.LogInformation("Peer {PeerId} completed torrent {TorrentId} (User: {UserId}).", peerId, torrent.Id, user.Id);
                if (peer != null)
                {
                    peer.LastAnnounce = DateTime.UtcNow;
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
        user.UploadedBytes += uploaded;
        user.DownloadedBytes += downloaded;
        _logger.LogInformation("User {UserId} stats updated: Uploaded {Uploaded}, Downloaded {Downloaded}.", user.Id, user.UploadedBytes, user.DownloadedBytes);

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
}