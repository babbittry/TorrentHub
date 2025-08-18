namespace TorrentHub.DTOs;

public class ForumTopicDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public int CategoryId { get; set; }
    
    public int AuthorId { get; set; }
    public string? AuthorName { get; set; }
    
    public int PostCount { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? LastPostTime { get; set; }
    
    public bool IsSticky { get; set; }
    public bool IsLocked { get; set; }
}
