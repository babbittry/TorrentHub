namespace TorrentHub.Core.DTOs;

/// <summary>
/// 批量处理响应
/// </summary>
public class BatchProcessResponse
{
    /// <summary>
    /// 成功处理的数量
    /// </summary>
    public int ProcessedCount { get; set; }
    
    /// <summary>
    /// 请求处理的总数
    /// </summary>
    public int TotalRequested { get; set; }
}