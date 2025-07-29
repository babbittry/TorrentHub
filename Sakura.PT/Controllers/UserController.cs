using Microsoft.AspNetCore.Mvc;
using Sakura.PT.DTOs;
using Sakura.PT.Mappers;
using Sakura.PT.Services;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserForRegistrationDto userForRegistrationDto)
    {
        try
        {
            var newUser = await _userService.RegisterAsync(userForRegistrationDto);
            var userDto = Mapper.ToUserDto(newUser);
            return CreatedAtAction(nameof(GetUser), new { id = userDto.Id }, userDto); // Assuming you will have a GetUser(id) endpoint
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
    {
        try
        {
            var loginResponse = await _userService.LoginAsync(userForLoginDto);
            return Ok(loginResponse);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // Placeholder for GetUser(id) - you can implement this later
    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        // In a real app, you would fetch the user from the database.
        return Ok(new { Id = id, Message = "GetUser endpoint is not implemented yet." });
    }
}