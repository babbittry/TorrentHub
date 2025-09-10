namespace TorrentHub.Core.DTOs;

public class ForumTopicDetailDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    
    public UserDisplayDto? Author { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public bool IsSticky { get; set; }
    public bool IsLocked { get; set; }

    public PaginatedResult<ForumPostDto>? Posts { get; set; }
}
