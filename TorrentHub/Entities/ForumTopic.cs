using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorrentHub.Entities;

/// <summary>
/// Represents a topic (thread) in the forum.
/// </summary>
public class ForumTopic
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)] // As discussed
    public required string Title { get; set; }

    [Required]
    public int AuthorId { get; set; }
    [ForeignKey(nameof(AuthorId))]
    public User? Author { get; set; }

    [Required]
    public int CategoryId { get; set; }
    [ForeignKey(nameof(CategoryId))]
    public ForumCategory? Category { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastPostTime { get; set; }

    [Required]
    public bool IsSticky { get; set; } = false;

    [Required]
    public bool IsLocked { get; set; } = false;
    
    public ICollection<ForumPost> Posts { get; set; } = new List<ForumPost>();
}
