namespace Sakura.PT.DTOs;

using Sakura.PT.Entities;

public class LoginResponseDto
{
    public required string Token { get; set; }
    public required User User { get; set; }
}
