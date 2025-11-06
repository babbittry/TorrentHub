namespace TorrentHub.Core.Enums;

/// <summary>
/// 媒体ID来源类型（支持电影、电视剧等）
/// </summary>
public enum MediaIdSource
{
    /// <summary>豆瓣ID (纯数字)</summary>
    DoubanId,
    
    /// <summary>豆瓣URL</summary>
    DoubanUrl,
    
    /// <summary>IMDb ID (tt开头)</summary>
    ImdbId,
    
    /// <summary>IMDb URL</summary>
    ImdbUrl,
    
    /// <summary>无法识别</summary>
    Unknown
}