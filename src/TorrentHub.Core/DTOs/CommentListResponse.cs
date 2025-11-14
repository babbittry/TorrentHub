namespace TorrentHub.Core.DTOs;

/// <summary>
/// 评论列表响应DTO
/// </summary>
public class CommentListResponse
{
    public List<CommentDto> Items { get; set; } = new();
    public bool HasMore { get; set; }
    public int TotalCount { get; set; }
    public int LoadedCount { get; set; }
}