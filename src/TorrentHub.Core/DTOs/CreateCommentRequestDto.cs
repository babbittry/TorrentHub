using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class CreateCommentRequestDto
{
    [Required]
    [StringLength(500)]
    public required string Text { get; set; }

    // Reply functionality fields
    public int? ParentCommentId { get; set; }
    public int? ReplyToUserId { get; set; }
}
