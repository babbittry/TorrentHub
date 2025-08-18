using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class UpdateForumTopicDto
{
    [Required]
    [StringLength(100)]
    public required string Title { get; set; }
}
