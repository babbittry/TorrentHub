using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class CreateAnnouncementRequestDto
{
    [Required]
    [StringLength(100)]
    public required string Title { get; set; }

    [Required]
    [StringLength(500)]
    public required string Content { get; set; }

    public bool SendToInbox { get; set; }
}
