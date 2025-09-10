namespace TorrentHub.Core.DTOs;

public class ForumPostDto
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public int Floor { get; set; }
    public required string Content { get; set; }
    public UserDisplayDto? Author { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
}
