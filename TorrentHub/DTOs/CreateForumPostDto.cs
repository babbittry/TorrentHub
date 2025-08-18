using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class CreateForumPostDto
{
    [Required]
    public required string Content { get; set; }
}
