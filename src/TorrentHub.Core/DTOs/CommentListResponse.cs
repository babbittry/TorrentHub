namespace TorrentHub.Core.DTOs;

/// <summary>
/// Response DTO for lazy-loading comment list with pagination info
/// </summary>
public class CommentListResponse
{
    public List<CommentDto> Comments { get; set; } = new();
    public bool HasMore { get; set; }
    public int TotalCount { get; set; }
    public int LoadedCount { get; set; }
}