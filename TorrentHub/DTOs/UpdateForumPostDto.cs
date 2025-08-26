using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class UpdateForumPostDto
{
    [Required]
    [StringLength(1000)]
    public required string Content { get; set; }
}
