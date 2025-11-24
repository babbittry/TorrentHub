namespace TorrentHub.Core.DTOs;

/// <summary>
/// Response DTO for lazy-loading torrent comment list with pagination info
/// </summary>
public class TorrentCommentListResponse
{
    public List<TorrentCommentDto> Items { get; set; } = new();
    public bool HasMore { get; set; }
    public int TotalItems { get; set; }
    public int LoadedCount { get; set; }
}