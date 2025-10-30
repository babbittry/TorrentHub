using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

/// <summary>
/// 更新RSS Feed Token请求
/// </summary>
public class UpdateRssFeedTokenRequest
{
    /// <summary>
    /// Feed类型 (可选)
    /// </summary>
    public RssFeedType? FeedType { get; set; }

    /// <summary>
    /// 分类筛选器 (可选)
    /// </summary>
    public string[]? CategoryFilter { get; set; }

    /// <summary>
    /// Token名称 (可选)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 最大返回结果数 (可选)
    /// </summary>
    public int? MaxResults { get; set; }

    /// <summary>
    /// 过期时间 (可选)
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}