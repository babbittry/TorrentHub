using System.ComponentModel.DataAnnotations;

namespace Sakura.PT.Entities;

/// <summary>
/// Represents a purchasable or unlockable badge.
/// </summary>
public class Badge
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(512)]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Indicates if this badge is available for purchase in the store.
    /// </summary>
    public bool IsPurchasable { get; set; } = false;
}
