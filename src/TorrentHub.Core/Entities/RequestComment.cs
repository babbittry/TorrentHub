using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorrentHub.Core.Entities;

public class RequestComment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(500)]
    public required string Text { get; set; }

    [Required]
    public int RequestId { get; set; }

    [ForeignKey(nameof(RequestId))]
    public Request? Request { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? EditedAt { get; set; }

    // Reply functionality fields
    [Required]
    public int Floor { get; set; }

    public int? ParentCommentId { get; set; }

    [ForeignKey(nameof(ParentCommentId))]
    public RequestComment? ParentRequestComment { get; set; }

    public int? ReplyToUserId { get; set; }

    [ForeignKey(nameof(ReplyToUserId))]
    public User? ReplyToUser { get; set; }

    [Required]
    public int Depth { get; set; } = 0;

    [Required]
    public int ReplyCount { get; set; } = 0;

    // Navigation property for replies
    public ICollection<RequestComment> Replies { get; set; } = new List<RequestComment>();
}