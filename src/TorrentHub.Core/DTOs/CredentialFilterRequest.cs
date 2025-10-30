namespace TorrentHub.Core.DTOs;

/// <summary>
/// Credential筛选请求参数
/// </summary>
public class CredentialFilterRequest
{
    /// <summary>
    /// 搜索关键字 (搜索种子名称)
    /// </summary>
    public string? SearchKeyword { get; set; }

    /// <summary>
    /// 是否包含已撤销的凭证
    /// </summary>
    public bool IncludeRevoked { get; set; } = false;

    /// <summary>
    /// 只显示已撤销的凭证
    /// </summary>
    public bool OnlyRevoked { get; set; } = false;

    /// <summary>
    /// 排序字段
    /// </summary>
    public CredentialSortBy SortBy { get; set; } = CredentialSortBy.CreatedAt;

    /// <summary>
    /// 排序方向
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;

    /// <summary>
    /// 页码 (从1开始)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 排序字段枚举
/// </summary>
public enum CredentialSortBy
{
    CreatedAt = 0,
    LastUsedAt = 1,
    UsageCount = 2,
    TorrentName = 3,
    TotalUploadedBytes = 4,
    TotalDownloadedBytes = 5
}

/// <summary>
/// 排序方向枚举
/// </summary>
public enum SortDirection
{
    Ascending = 0,
    Descending = 1
}