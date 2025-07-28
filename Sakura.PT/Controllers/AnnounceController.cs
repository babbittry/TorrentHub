using Microsoft.AspNetCore.Mvc;
using Sakura.PT.Data;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("[controller]")]
public class AnnounceController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AnnounceController(ApplicationDbContext context)
    {
        _context = context;
    }

    /*[HttpGet]
    public IActionResult Get(string info_hash, string peer_id, int port, long uploaded, long downloaded, long left)
    {
        // 1. Find the torrent
        var torrent = _context.Torrents.FirstOrDefault(t => t.InfoHash == info_hash);
        if (torrent == null)
        {
            return NotFound("Torrent not found.");
        }

        // 2. Update user stats (simplified)
        // You'll need a way to identify the user, perhaps via a passkey in the announce URL
        // For now, let's assume we have the user's ID
        var userId = 1; // Placeholder
        var user = _context.Users.Find(userId);
        if (user != null)
        {
            user.Uploaded += uploaded;
            user.Downloaded += downloaded;
            _context.SaveChanges();
        }

        // 3. Get peers
        var peers = _context.Peers
            .Where(p => p.TorrentId == torrent.Id && p.PeerId != peer_id)
            .ToList();

        // 4. Add or update the current peer
        var currentPeer = _context.Peers.FirstOrDefault(p => p.PeerId == peer_id && p.TorrentId == torrent.Id);
        if (currentPeer == null)
        {
            currentPeer = new Peer
            {
                PeerId = peer_id,
                IpAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                Port = port,
                TorrentId = torrent.Id,
                UserId = userId
            };
            _context.Peers.Add(currentPeer);
        }

        currentPeer.LastAnnounce = DateTime.UtcNow;
        _context.SaveChanges();

        // 5. Build the response (Bencode format)
        var response = new BencodeDictionary
        {
            { "interval", new BencodeInteger(1800) }, // 30 minutes
            {
                "peers", new BencodeList(peers.Select(p => new BencodeDictionary
                {
                    { "peer id", new BencodeString(p.PeerId) },
                    { "ip", new BencodeString(p.IpAddress) },
                    { "port", new BencodeInteger(p.Port) }
                }))
            }
        };

        return Content(response.Encode(), "text/plain");
    }*/
}