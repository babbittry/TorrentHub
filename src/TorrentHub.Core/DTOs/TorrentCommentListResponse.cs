namespace TorrentHub.Core.DTOs;

/// <summary>
/// Response DTO for lazy-loading torrent comment list with pagination info
/// </summary>
public class TorrentCommentListResponse
{
    public List<TorrentCommentDto> TorrentComments { get; set; } = new();
    public bool HasMore { get; set; }
    public int TotalCount { get; set; }
    public int LoadedCount { get; set; }
}