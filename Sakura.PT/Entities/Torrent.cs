using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using Sakura.PT.Enums;

namespace Sakura.PT.Entities;

/// <summary>
/// Represents a torrent file in the system.
/// </summary>
public class Torrent
{
    /// <summary>
    /// Unique identifier for the torrent.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Name of the torrent.
    /// </summary>
    [Required]
    [StringLength(255)]
    public required string Name { get; set; }

    /// <summary>
    /// The SHA1 hash of the torrent's info dictionary.
    /// </summary>
    [Required]
    [StringLength(40)]
    public required string InfoHash { get; set; }

    /// <summary>
    /// Path to the stored .torrent file.
    /// </summary>
    [Required]
    [StringLength(512)]
    public required string FilePath { get; set; }

    /// <summary>
    /// Description of the torrent content.
    /// </summary>
    [StringLength(4096)]
    public string? Description { get; set; }

    /// <summary>
    /// Foreign key of the user who uploaded the torrent.
    /// </summary>
    [Required]
    public int UploadedByUserId { get; set; }

    /// <summary>
    /// Navigation property for the uploader.
    /// </summary>
    [ForeignKey(nameof(UploadedByUserId))]
    public required User UploadedByUser { get; set; }

    /// <summary>
    /// Category of the torrent.
    /// </summary>
    [Required]
    public TorrentCategory Category { get; set; }

    /// <summary>
    /// Total size of the torrent content in bytes.
    /// </summary>
    [Required]
    public long Size { get; set; }

    /// <summary>
    /// Indicates if the torrent has been deleted.
    /// </summary>
    [Required]
    [DefaultValue(false)]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Timestamp when the torrent was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}