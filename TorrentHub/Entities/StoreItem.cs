using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TorrentHub.Enums;

namespace TorrentHub.Entities;

/// <summary>
/// Represents an item available for purchase in the store.
/// </summary>
public class StoreItem
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// A unique code to identify the item type programmatically.
    /// </summary>
        [Column(TypeName = "store_item_code")]
    public required StoreItemCode ItemCode { get; set; }

    [Required]
    public ulong Price { get; set; }

    /// <summary>
    /// Whether the item is currently available for purchase.
    /// </summary>
    [Required]
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Optional: The ID of the badge granted if this item is a badge.
    /// </summary>
    public int? BadgeId { get; set; }

    [ForeignKey(nameof(BadgeId))]
    public Badge? Badge { get; set; }
}
