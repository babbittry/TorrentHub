using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TorrentHub.Core.Enums;

namespace TorrentHub.Core.Entities;

/// <summary>
/// 统一的评论实体（支持种子、求种、论坛等多种类型）
/// </summary>
public class Comment
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 评论目标类型
    /// </summary>
    [Required]
    [Column(TypeName = "text")]
    public CommentableType CommentableType { get; set; }

    /// <summary>
    /// 评论目标ID
    /// </summary>
    [Required]
    public int CommentableId { get; set; }

    /// <summary>
    /// 评论用户ID
    /// </summary>
    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// 评论内容
    /// </summary>
    [Required]
    [StringLength(1000)]
    public required string Content { get; set; }

    /// <summary>
    /// 楼层号
    /// </summary>
    [Required]
    public int Floor { get; set; }

    /// <summary>
    /// 父评论ID
    /// </summary>
    public int? ParentCommentId { get; set; }

    [ForeignKey(nameof(ParentCommentId))]
    public Comment? ParentComment { get; set; }

    /// <summary>
    /// 回复的用户ID
    /// </summary>
    public int? ReplyToUserId { get; set; }

    [ForeignKey(nameof(ReplyToUserId))]
    public User? ReplyToUser { get; set; }

    /// <summary>
    /// 评论层级深度
    /// </summary>
    [Required]
    public int Depth { get; set; } = 0;

    /// <summary>
    /// 回复数量
    /// </summary>
    [Required]
    public int ReplyCount { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 编辑时间
    /// </summary>
    public DateTimeOffset? EditedAt { get; set; }

    /// <summary>
    /// 回复列表（导航属性）
    /// </summary>
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}