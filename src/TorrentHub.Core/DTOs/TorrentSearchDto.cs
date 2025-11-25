using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

public class TorrentSearchDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Subtitle { get; set; }
    public TorrentCategory Category { get; set; }
    public long Size { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsFree { get; set; }
    public DateTimeOffset? FreeUntil { get; set; }
    public TorrentStickyStatus StickyStatus { get; set; }
    public int Snatched { get; set; }

    // TMDb Fields
    public string? ImdbId { get; set; }
    public int? TMDbId { get; set; }
    public string? OriginalTitle { get; set; }
    public int? Year { get; set; }
    public int? Runtime { get; set; }
    public List<string>? Genres { get; set; }
    public string? Directors { get; set; }
    public string? Cast { get; set; }
}

