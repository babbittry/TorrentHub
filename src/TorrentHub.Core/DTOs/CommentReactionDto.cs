using TorrentHub.Core.Enums;

namespace TorrentHub.Core.DTOs;

/// <summary>
/// 评论表情回应DTO
/// </summary>
public class CommentReactionDto
{
    /// <summary>
    /// 回应ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 评论ID
    /// </summary>
    public int CommentId { get; set; }

    /// <summary>
    /// 评论类型
    /// </summary>
    public string CommentType { get; set; } = "Comment";

    /// <summary>
    /// 表情类型
    /// </summary>
    public ReactionType Type { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 用户头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}