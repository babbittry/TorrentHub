using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class UpdateForumPostDto
{
    [Required]
    public required string Content { get; set; }
}
