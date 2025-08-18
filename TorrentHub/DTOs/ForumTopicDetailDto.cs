namespace TorrentHub.DTOs;

public class ForumTopicDetailDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    
    public int AuthorId { get; set; }
    public string? AuthorName { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public bool IsSticky { get; set; }
    public bool IsLocked { get; set; }

    public List<ForumPostDto> Posts { get; set; } = new();
}
