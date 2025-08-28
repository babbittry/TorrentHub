namespace TorrentHub.Core.DTOs;

public class CommentDto
{
    public int Id { get; set; }
    public required string Text { get; set; }
    public int TorrentId { get; set; }
    public UserPublicProfileDto? User { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
}
