namespace TorrentHub.Core.DTOs;

/// <summary>
/// Represents an active P2P connection (a peer) for a user.
/// </summary>
public class PeerDto
{
    public int TorrentId { get; set; }
    public required string TorrentName { get; set; }
    public required string UserAgent { get; set; }
    public required string IpAddress { get; set; }
    public int Port { get; set; }
    public ulong Uploaded { get; set; }
    public ulong Downloaded { get; set; }
    public bool IsSeeder { get; set; }
    public DateTimeOffset LastAnnounceAt { get; set; }
}
