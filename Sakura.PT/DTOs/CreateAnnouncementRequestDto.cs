using System.ComponentModel.DataAnnotations;

namespace Sakura.PT.DTOs;

public class CreateAnnouncementRequestDto
{
    [Required]
    public required string Title { get; set; }

    [Required]
    public required string Content { get; set; }

    public bool SendToInbox { get; set; }
}
