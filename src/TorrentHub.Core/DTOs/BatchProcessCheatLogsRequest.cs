namespace TorrentHub.Core.DTOs;

/// <summary>
/// 批量处理CheatLog请求
/// </summary>
public class BatchProcessCheatLogsRequest
{
    /// <summary>
    /// CheatLog ID列表
    /// </summary>
    public required int[] LogIds { get; set; }
    
    /// <summary>
    /// 管理员备注
    /// </summary>
    public string? Notes { get; set; }
}