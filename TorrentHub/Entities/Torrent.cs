using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using NpgsqlTypes;
using TorrentHub.Enums;

namespace TorrentHub.Entities;

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

    /// <summary>
    /// Indicates if the torrent is free to download.
    /// </summary>
    [Required]
    [DefaultValue(false)]
    public bool IsFree { get; set; } = false;

    /// <summary>
    /// The date and time until which the torrent is free. Null if not free.
    /// </summary>
    public DateTime? FreeUntil { get; set; }

    /// <summary>
    /// The sticky status of the torrent.
    /// </summary>
    [Required]
    [DefaultValue(TorrentStickyStatus.None)]
    public TorrentStickyStatus StickyStatus { get; set; } = TorrentStickyStatus.None;

    /// <summary>
    /// Number of times the torrent has been snatched (completed).
    /// </summary>
    [Required]
    [DefaultValue(0)]
    public int Snatched { get; set; } = 0;
    
    /// <summary>
    /// Number of seeders for this torrent.
    /// </summary>
    [Required]
    [DefaultValue(0)]
    public int Seeders { get; set; } = 0;

    /// <summary>
    /// Number of leechers for this torrent.
    /// </summary>
    [Required]
    [DefaultValue(0)]
    public int Leechers { get; set; } = 0;

    // --- TMDb Fields ---

    [StringLength(15)]
    public string? ImdbId { get; set; }

    public int? TMDbId { get; set; }

    [StringLength(255)]
    public string? OriginalTitle { get; set; }

    [StringLength(1024)]
    public string? Tagline { get; set; }
    
    public int? Year { get; set; }

    [StringLength(255)]
    public string? PosterPath { get; set; }

    [StringLength(255)]
    public string? BackdropPath { get; set; }
    
    public int? Runtime { get; set; }

    [StringLength(255)]
    public string? Genres { get; set; }

    [StringLength(255)]
    public string? Directors { get; set; }

    [StringLength(1024)]
    public string? Cast { get; set; }

    public double? Rating { get; set; }
}
