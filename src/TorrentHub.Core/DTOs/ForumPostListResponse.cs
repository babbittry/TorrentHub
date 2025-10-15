namespace TorrentHub.Core.DTOs;

/// <summary>
/// Response DTO for lazy-loading forum post list with pagination info
/// </summary>
public class ForumPostListResponse
{
    public List<ForumPostDto> Posts { get; set; } = new();
    public bool HasMore { get; set; }
    public int TotalCount { get; set; }
    public int LoadedCount { get; set; }
}