using System.ComponentModel.DataAnnotations;
using TorrentHub.Enums;

namespace TorrentHub.Entities;

/// <summary>
/// Represents a purchasable or unlockable badge.
/// </summary>
public class Badge
{
    [Key]
    public int Id { get; set; }

    [Required]
    public BadgeCode Code { get; set; }
}
