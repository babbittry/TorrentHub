namespace TorrentHub.DTOs;

public class ForumPostDto
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public required string Content { get; set; }
    public int AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorAvatar { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
}
