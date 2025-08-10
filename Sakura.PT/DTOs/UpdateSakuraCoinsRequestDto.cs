using System.ComponentModel.DataAnnotations;

namespace Sakura.PT.DTOs;

public class UpdateSakuraCoinsRequestDto
{
    [Required]
    public ulong Amount { get; set; }
}
