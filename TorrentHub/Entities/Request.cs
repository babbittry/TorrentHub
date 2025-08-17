using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TorrentHub.Enums;

namespace TorrentHub.Entities;

public class Request
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public required string Title { get; set; }

    [Required]
    [StringLength(500)]
    public required string Description { get; set; }

    [Required]
    public int RequestedByUserId { get; set; }

    [ForeignKey(nameof(RequestedByUserId))]
    public User? RequestedByUser { get; set; }

    public int? FilledByUserId { get; set; }

    [ForeignKey(nameof(FilledByUserId))]
    public User? FilledByUser { get; set; }

    public int? FilledWithTorrentId { get; set; }

    [ForeignKey(nameof(FilledWithTorrentId))]
    public Torrent? FilledWithTorrent { get; set; }

    [Required]
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? FilledAt { get; set; }

    /// <summary>
    /// The total amount of Coins offered as a bounty for this request.
    /// </summary>
    [Required]
    public ulong BountyAmount { get; set; } = 0UL;
}
