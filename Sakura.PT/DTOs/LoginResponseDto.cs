namespace Sakura.PT.DTOs;

public class LoginResponseDto
{
    public required string Token { get; set; }
    public required UserDto User { get; set; }
}
