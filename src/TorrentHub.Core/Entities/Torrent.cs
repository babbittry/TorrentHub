using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using NpgsqlTypes;
using TorrentHub.Core.Enums;
using TorrentHub.Core.DTOs;

namespace TorrentHub.Core.Entities;

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
    public required byte[] InfoHash { get; set; }

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

    [Column(TypeName = "torrent_category")]
    public required TorrentCategory Category { get; set; }

    /// <summary>
    /// Size of the torrent in bytes.
    /// </summary>
    [Required]
    public long Size { get; set; }

    /// <summary>
    /// Indicates if the torrent is deleted.
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Reason for deletion, if applicable.
    /// </summary>
    public TorrentDeleteReason? DeleteReason { get; set; }

    /// <summary>
    /// Timestamp when the torrent was created.
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Indicates if the torrent is free to download (no download stats recorded).
    /// </summary>
    [Required]
    public bool IsFree { get; set; }

    /// <summary>
    /// Timestamp until the torrent is free.
    /// </summary>
    public DateTimeOffset? FreeUntil { get; set; }

    /// <summary>
    /// Sticky status of the torrent on the torrent list.
    /// </summary>
    [Required]
    [Column(TypeName = "torrent_sticky_status")]
    public required TorrentStickyStatus StickyStatus { get; set; }

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

    public required List<string> Genres { get; set; } = new();

    [StringLength(255)]
    public string? Directors { get; set; }

    [Column(TypeName = "jsonb")]
    public List<CastMemberDto>? Cast { get; set; }

    public double? Rating { get; set; }
    
    // Technical Specs Fields
    [StringLength(50)]
    public string? Resolution { get; set; }
    
    [StringLength(50)]
    public string? VideoCodec { get; set; }
    
    [StringLength(50)]
    public string? AudioCodec { get; set; }
    
    [StringLength(100)]
    public string? Subtitles { get; set; }
    
    [StringLength(50)]
    public string? Source { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }
}

