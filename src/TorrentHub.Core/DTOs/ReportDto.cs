using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

public class ReportDto
{
    public int Id { get; set; }
    public TorrentDto? Torrent { get; set; }
    public UserPublicProfileDto? ReporterUser { get; set; }
    public ReportReason Reason { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset ReportedAt { get; set; }
    public bool IsProcessed { get; set; }
    public UserPublicProfileDto? ProcessedByUser { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? AdminNotes { get; set; }
}

