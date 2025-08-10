using System.ComponentModel.DataAnnotations;

namespace Sakura.PT.DTOs;

public class CreateCommentRequestDto
{
    [Required]
    public required string Text { get; set; }
}
