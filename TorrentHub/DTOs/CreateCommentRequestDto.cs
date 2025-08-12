using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class CreateCommentRequestDto
{
    [Required]
    public required string Text { get; set; }
}
