using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorrentHub.Entities;

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
    public int AuthorId { get; set; }
    [ForeignKey(nameof(AuthorId))]
    public User? Author { get; set; }

    [Required]
    public required string Content { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EditedAt { get; set; }
}
