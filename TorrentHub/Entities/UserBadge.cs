using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorrentHub.Entities;

/// <summary>
/// Represents a badge owned by a user.
/// </summary>
public class UserBadge
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    public int BadgeId { get; set; }

    [ForeignKey(nameof(BadgeId))]
    public Badge? Badge { get; set; }

    [Required]
    public DateTimeOffset AcquiredAt { get; set; } = DateTimeOffset.UtcNow;
}
