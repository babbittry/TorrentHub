using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorrentHub.Core.Entities;

/// <summary>
/// Represents a single post (reply) within a forum topic.
/// </summary>
public class ForumPost
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TopicId { get; set; }
    [ForeignKey(nameof(TopicId))]
    public ForumTopic? Topic { get; set; }

    [Required]
    public int Floor { get; set; }

    [Required]
    public int AuthorId { get; set; }
    [ForeignKey(nameof(AuthorId))]
    public User? Author { get; set; }

    [Required]
    [StringLength(1000)]
    public required string Content { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? EditedAt { get; set; }

    // Reply functionality fields
    public int? ParentPostId { get; set; }

    [ForeignKey(nameof(ParentPostId))]
    public ForumPost? ParentPost { get; set; }

    public int? ReplyToUserId { get; set; }

    [ForeignKey(nameof(ReplyToUserId))]
    public User? ReplyToUser { get; set; }

    [Required]
    public int Depth { get; set; } = 0;

    [Required]
    public int ReplyCount { get; set; } = 0;

    // Navigation property for replies
    public ICollection<ForumPost> Replies { get; set; } = new List<ForumPost>();
}
