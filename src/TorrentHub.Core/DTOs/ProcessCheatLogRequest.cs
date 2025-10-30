namespace TorrentHub.Core.DTOs;

/// <summary>
/// 处理CheatLog请求
/// </summary>
public class ProcessCheatLogRequest
{
    /// <summary>
    /// 管理员备注
    /// </summary>
    public string? Notes { get; set; }
}