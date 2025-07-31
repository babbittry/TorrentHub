using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sakura.PT.Entities;

/// <summary>
/// Represents a peer in a torrent swarm.
/// </summary>
public class Peers
{
    /// <summary>
    /// Unique identifier for the peer entry.
    /// </summary>
    [Key]
    public int Id { get; set; }

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
    [StringLength(45)] // Accommodates IPv6 addresses
    public required string IpAddress { get; set; }

    /// <summary>
    /// Port number the peer is listening on.
    /// </summary>
    [Required]
    public required int Port { get; set; }

    /// <summary>
    /// Timestamp of the last announce from this peer.
    /// </summary>
    [Required]
    public DateTime LastAnnounce { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if the peer is a seeder (true) or a Mosquito (false).
    /// </summary>
    [Required]
    public bool IsSeeder { get; set; }
}