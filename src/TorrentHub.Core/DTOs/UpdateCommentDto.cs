using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

/// <summary>
/// 更新评论请求DTO
/// </summary>
public class UpdateCommentDto
{
    [Required]
    [StringLength(1000)]
    public required string Content { get; set; }
}