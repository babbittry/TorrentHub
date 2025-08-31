using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorrentHub.Core.Entities;

public class StoreItemTranslation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StoreItemId { get; set; }

    [ForeignKey(nameof(StoreItemId))]
    public StoreItem StoreItem { get; set; } = null!;

    /// <summary>
    /// The language code (e.g., "zh-CN", "fr", "ja").
    /// </summary>
    [Required]
    [MaxLength(10)]
    public required string Language { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(500)]
    public required string Description { get; set; }
}