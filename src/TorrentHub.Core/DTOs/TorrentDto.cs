using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

public class TorrentDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Plot { get; set; }
    public string? Subtitle { get; set; }
    public bool IsAnonymous { get; set; }
    public string? MediaInfo { get; set; }
    public long Size { get; set; }
    public UserDisplayDto? Uploader { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public TorrentCategory Category { get; set; }
    public bool IsFree { get; set; }
    public DateTimeOffset? FreeUntil { get; set; }
    public TorrentStickyStatus StickyStatus { get; set; }
    public bool IsDeleted { get; set; }
    public TorrentDeleteReason? DeleteReason { get; set; }
    public int Seeders { get; set; }
    public int Leechers { get; set; }
    public int Snatched { get; set; }

    // TMDb Fields
    public string? ImdbId { get; set; }
    public int? TMDbId { get; set; }
    public string? OriginalTitle { get; set; }
    public int? Year { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public int? Runtime { get; set; }
    public List<string>? Genres { get; set; }
    public string? Directors { get; set; }
    public List<CastMemberDto>? Cast { get; set; }
    public double? Rating { get; set; }
    
    // New Fields
    public double? ImdbRating { get; set; }
    public TechnicalSpecsDto? TechnicalSpecs { get; set; }
    public List<TorrentFileDto>? Files { get; set; }
    public string? Country { get; set; }
    
    /// <summary>
    /// 截图 URL 列表
    /// </summary>
    public List<string> Screenshots { get; set; } = new();
}

