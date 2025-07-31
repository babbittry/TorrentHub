using Microsoft.AspNetCore.Mvc;
using Sakura.PT.DTOs;
using Sakura.PT.Mappers;
using Sakura.PT.Services;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        _logger.LogInformation("GetUser request received for id: {Id}", id);
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound("User not found.");
        }
        return Ok(Mapper.ToUserDto(user));
    }

    [HttpGet("{userId}/badges")]
    public async Task<IActionResult> GetUserBadges(int userId)
    {
        var badges = await _userService.GetUserBadgesAsync(userId);
        return Ok(badges);
    }

    [HttpGet("mybadges")]
    [Authorize]
    public async Task<IActionResult> GetMyBadges()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var badges = await _userService.GetUserBadgesAsync(userId);
        return Ok(badges);
    }
}
