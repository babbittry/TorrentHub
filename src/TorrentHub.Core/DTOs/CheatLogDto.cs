namespace TorrentHub.Core.DTOs;

public class CheatLogDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    
    // 新增字段
    public int? TorrentId { get; set; }
    public string? TorrentName { get; set; }
    public required string DetectionType { get; set; }
    public string? IpAddress { get; set; }
    
    // 原有字段
    public DateTimeOffset Timestamp { get; set; }
    public required string Reason { get; set; }
    public string? Details { get; set; }
}
