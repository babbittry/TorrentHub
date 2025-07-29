using System.ComponentModel.DataAnnotations;

namespace Sakura.PT.DTOs;

public class UserForLoginDto
{
    [Required]
    public required string UserName { get; set; }

    [Required]
    public required string Password { get; set; }
}
