namespace Sakura.PT.DTOs;

public class CommentDto
{
    public int Id { get; set; }
    public required string Text { get; set; }
    public int TorrentId { get; set; }
    public UserPublicProfileDto? User { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
}