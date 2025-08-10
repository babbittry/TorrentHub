using System.ComponentModel.DataAnnotations;

namespace Sakura.PT.DTOs;

public class CompleteInfoRequestDto
{
    [Required]
    public required string ImdbId { get; set; }
}
