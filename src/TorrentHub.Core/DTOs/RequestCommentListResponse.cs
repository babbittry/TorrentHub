namespace TorrentHub.Core.DTOs;

/// <summary>
/// Response DTO for lazy-loading request comment list with pagination info
/// </summary>
public class RequestCommentListResponse
{
    public List<RequestCommentDto> Items { get; set; } = new();
    public bool HasMore { get; set; }
    public int TotalItems { get; set; }
    public int LoadedCount { get; set; }
}