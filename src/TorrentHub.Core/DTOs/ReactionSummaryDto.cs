using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

/// <summary>
/// 表情反应汇总
/// </summary>
public class ReactionSummaryDto
{
    public ReactionType Type { get; set; }
    public int Count { get; set; }
    public bool ViewerReacted { get; set; }
    public List<UserDisplayDto> Users { get; set; } = new();
}

/// <summary>
/// 评论的所有反应
/// </summary>
public class CommentReactionsDto
{
    public int TotalItems { get; set; }
    public List<ReactionSummaryDto> Reactions { get; set; } = new();
}

/// <summary>
/// 批量获取反应请求
/// </summary>
public class GetReactionsBatchRequestDto
{
    public required List<int> CommentIds { get; set; }
}