using TorrentHub.Enums;

namespace TorrentHub.DTOs;

public class ReportDto
{
    public int Id { get; set; }
    public TorrentDto? Torrent { get; set; }
    public UserPublicProfileDto? ReporterUser { get; set; }
    public ReportReason Reason { get; set; }
    public string? Details { get; set; }
    public DateTime ReportedAt { get; set; }
    public bool IsProcessed { get; set; }
    public UserPublicProfileDto? ProcessedByUser { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? AdminNotes { get; set; }
}
