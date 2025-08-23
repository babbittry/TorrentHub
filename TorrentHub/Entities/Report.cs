using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TorrentHub.Enums;

namespace TorrentHub.Entities;

/// <summary>
/// Represents a report filed against a torrent.
/// </summary>
public class Report
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TorrentId { get; set; }

    [ForeignKey(nameof(TorrentId))]
    public Torrent? Torrent { get; set; }

    [Required]
    public int ReporterUserId { get; set; }

    [ForeignKey(nameof(ReporterUserId))]
    public User? ReporterUser { get; set; }

    [Column(TypeName = "report_reason")]
    public required ReportReason Reason { get; set; }

    [StringLength(1000)]
    public string? Details { get; set; }

    [Required]
    public DateTimeOffset ReportedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Indicates if the report has been processed by an administrator.
    /// </summary>
    [Required]
    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// The ID of the administrator who processed the report.
    /// </summary>
    public int? ProcessedByUserId { get; set; }

    [ForeignKey(nameof(ProcessedByUserId))]
    public User? ProcessedByUser { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    [StringLength(1000)]
    public string? AdminNotes { get; set; }
}
