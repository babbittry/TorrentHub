using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorrentHub.Core.Entities;

/// <summary>
/// Represents a peer in a torrent swarm.
/// </summary>
public class Peers
{
    /// <summary>
    /// Unique identifier for the peer entry.
    /// </summary>
    [Key]
    public int Id { get; init; }

    /// <summary>
    /// Foreign key of the torrent this peer is part of.
    /// </summary>
    [Required]
    public int TorrentId { get; set; }

    /// <summary>
    /// Navigation property for the torrent.
    /// </summary>
    [ForeignKey(nameof(TorrentId))]
    public required Torrent Torrent { get; set; }

    /// <summary>
    /// Foreign key of the user (peer).
    /// </summary>
    [Required]
    public int UserId { get; init; }

    /// <summary>
    /// Navigation property for the user.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public required User User { get; init; }

    /// <summary>
    /// IP address of the peer.
    /// </summary>
    [Required]
    public required System.Net.IPAddress IpAddress { get; set; }

    /// <summary>
    /// Port number the peer is listening on.
    /// </summary>
    [Required]
    public required int Port { get; set; }

    /// <summary>
    /// Timestamp of the last announce from this peer.
    /// </summary>
    [Required]
    public DateTimeOffset LastAnnounce { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Indicates if the peer is a seeder (true) or a Mosquito (false).
    /// </summary>
    [Required]
    public bool IsSeeder { get; set; }

    /// <summary>
    /// The client user agent (peer_id) reported by the peer.
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string UserAgent { get; set; } = "";

    /// <summary>
    /// The total uploaded amount reported by the peer at the last announce.
    /// </summary>
    [Required]
    public ulong Uploaded { get; set; } = 0;

    /// <summary>
    /// The total downloaded amount reported by the peer at the last announce.
    /// </summary>
    [Required]
    public ulong Downloaded { get; set; } = 0;

    /// <summary>
    /// The credential used by this peer to connect to the tracker.
    /// This will be null for peers that connected before the credential system was implemented.
    /// </summary>
    public Guid? Credential { get; set; }
}
