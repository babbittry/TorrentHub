using System.ComponentModel.DataAnnotations;

namespace Sakura.PT.DTOs;

public class SendMessageRequestDto
{
    [Required]
    public int ReceiverId { get; set; }

    [Required]
    public required string Subject { get; set; }

    [Required]
    public required string Content { get; set; }
}
