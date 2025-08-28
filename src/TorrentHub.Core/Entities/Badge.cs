using System.ComponentModel.DataAnnotations;
using TorrentHub.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;
namespace TorrentHub.Core.Entities;

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

