using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

/// <summary>
/// 统一的评论DTO
/// </summary>
public class CommentDto
{
    public int Id { get; set; }
    public CommentableType CommentableType { get; set; }
    public int CommentableId { get; set; }
    public int UserId { get; set; }
    public UserDisplayDto? User { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Floor { get; set; }
    public int? ParentCommentId { get; set; }
    public int? ReplyToUserId { get; set; }
    public UserDisplayDto? ReplyToUser { get; set; }
    public int Depth { get; set; }
    public int ReplyCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
    
    // Reaction functionality (optional, can be loaded separately)
    public CommentReactionsDto? Reactions { get; set; }
}