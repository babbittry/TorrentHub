using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class UpdateRequestCommentRequestDto
{
    [Required]
    [StringLength(500)]
    public required string Content { get; set; }
}