using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class CreateForumPostDto
{
    [Required]
    [StringLength(1000)]
    public required string Content { get; set; }
}
