using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class CreateCommentRequestDto
{
    [Required]
    [StringLength(500)]
    public required string Text { get; set; }
}
