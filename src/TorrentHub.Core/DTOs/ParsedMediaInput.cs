using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

public class ParsedMediaInput
{
    /// <summary>输入来源类型</summary>
    public MediaIdSource Source { get; set; }
    
    /// <summary>提取的ID (豆瓣或IMDb)</summary>
    public string? Id { get; set; }
    
    /// <summary>是否有效</summary>
    public bool IsValid { get; set; }
    
    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }
}