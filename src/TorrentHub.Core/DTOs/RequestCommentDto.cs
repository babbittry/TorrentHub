namespace TorrentHub.Core.DTOs;

public class RequestCommentDto
{
    public int Id { get; set; }
    public required string Text { get; set; }
    public int RequestId { get; set; }
    public UserDisplayDto? User { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }

    // Reply functionality fields
    public int Floor { get; set; }
    public int? ParentCommentId { get; set; }
    public UserDisplayDto? ReplyToUser { get; set; }
    public int Depth { get; set; }
    public int ReplyCount { get; set; }

    // Reaction functionality
    public CommentReactionsDto? Reactions { get; set; }
}