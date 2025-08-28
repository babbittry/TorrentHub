using System.ComponentModel.DataAnnotations;

namespace TorrentHub.Core.DTOs;

public class CreateForumTopicDto
{
    [Required]
    [StringLength(100)] // From our discussion
    public required string Title { get; set; }

    [Required]
    public required string Content { get; set; }

    [Required]
    public int CategoryId { get; set; }
}
