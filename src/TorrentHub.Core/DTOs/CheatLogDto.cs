using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

public class CheatLogDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public int? TorrentId { get; set; }
    public string? TorrentName { get; set; }
    public CheatDetectionType DetectionType { get; set; }
    public CheatSeverity Severity { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    
    // 处理状态字段
    public bool IsProcessed { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int? ProcessedByUserId { get; set; }
    public string? ProcessedByUsername { get; set; }
    public string? AdminNotes { get; set; }
    public string? Details { get; set; }
}
