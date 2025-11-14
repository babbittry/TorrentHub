using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TorrentHub.Core.Enums;

namespace TorrentHub.Core.Entities;

/// <summary>
/// 评论表情反应
/// </summary>
public class CommentReaction
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 评论类型：TorrentComment、ForumPost 或 RequestComment
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string CommentType { get; set; }

    /// <summary>
    /// 评论ID
    /// </summary>
    [Required]
    public int CommentId { get; set; }

    /// <summary>
    /// 反应用户ID
    /// </summary>
    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// 表情类型
    /// </summary>
    [Required]
    public ReactionType Type { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}