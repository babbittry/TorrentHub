using System.ComponentModel.DataAnnotations;

namespace TorrentHub.DTOs;

public class CreateAnnouncementRequestDto
{
    [Required]
    public required string Title { get; set; }

    [Required]
    public required string Content { get; set; }

    public bool SendToInbox { get; set; }
}
