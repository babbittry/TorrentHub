using System.ComponentModel.DataAnnotations;
using TorrentHub.Enums;
using System.ComponentModel.DataAnnotations.Schema;
namespace TorrentHub.Entities;

/// <summary>
/// Represents a purchasable or unlockable badge.
/// </summary>
public class Badge
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "badge_code")]
    public required BadgeCode Code { get; set; }
}
