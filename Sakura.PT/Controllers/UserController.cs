using Microsoft.AspNetCore.Mvc;
using Sakura.PT.DTOs;
using Sakura.PT.Mappers;
using Sakura.PT.Services;
using Microsoft.Extensions.Logging;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserForRegistrationDto userForRegistrationDto)
    {
        _logger.LogInformation("Register request received for user: {UserName}", userForRegistrationDto.UserName);
        try
        {
            var newUser = await _userService.RegisterAsync(userForRegistrationDto);
            var userDto = Mapper.ToUserDto(newUser);
            _logger.LogInformation("User {UserName} registered successfully.", userForRegistrationDto.UserName);
            return CreatedAtAction(nameof(GetUser), new { id = userDto.Id }, userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for user {UserName}: {ErrorMessage}", userForRegistrationDto.UserName, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
    {
        _logger.LogInformation("Login request received for user: {UserName}", userForLoginDto.UserName);
        try
        {
            var loginResponse = await _userService.LoginAsync(userForLoginDto);
            _logger.LogInformation("User {UserName} logged in successfully.", userForLoginDto.UserName);
            return Ok(loginResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for user {UserName}: {ErrorMessage}", userForLoginDto.UserName, ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
    }

    // Placeholder for GetUser(id) - you can implement this later
    [HttpGet("{id:int}")]
    public IActionResult GetUser(int id)
    {
        _logger.LogInformation("GetUser request received for id: {Id}", id);
        // In a real app, you would fetch the user from the database.
        _logger.LogWarning("GetUser endpoint is not fully implemented yet.");
        return Ok(new { Id = id, Message = "GetUser endpoint is not implemented yet." });
    }
}