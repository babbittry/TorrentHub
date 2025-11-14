using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

/// <summary>
/// 创建评论请求DTO
/// </summary>
public class CreateCommentDto
{
    [Required]
    [StringLength(1000)]
    public required string Content { get; set; }
    
    public int? ParentCommentId { get; set; }
    
    public int? ReplyToUserId { get; set; }
}