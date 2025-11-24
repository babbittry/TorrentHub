using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

/// <summary>
/// 精简的求种信息 DTO，用于列表展示。
/// 相比完整的 RequestDto，移除了详情页才需要的字段，减少数据传输量。
/// </summary>
public class RequestSummaryDto
{
    public int Id { get; set; }
    
    public required string Title { get; set; }
    
    public ulong BountyAmount { get; set; }
    
    public RequestStatus Status { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// 发起求种的用户信息（包含用户名、头像、等级、徽章等展示所需的完整信息）
    /// </summary>
    public required UserDisplayDto RequestedByUser { get; set; }
}