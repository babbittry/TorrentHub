using TorrentHub.Enums;

namespace TorrentHub.DTOs;

public class TorrentDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public long Size { get; set; }
    public required string UploaderUsername { get; set; } 
    public DateTime CreatedAt { get; set; }
    public bool IsFree { get; set; }
    public DateTime? FreeUntil { get; set; }
    public TorrentStickyStatus StickyStatus { get; set; }
    public string? ImdbId { get; set; }
}