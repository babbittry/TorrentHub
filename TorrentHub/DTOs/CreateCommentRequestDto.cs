using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class CreateCommentRequestDto
{
    [Required]
    [StringLength(500)]
    public required string Text { get; set; }
}
