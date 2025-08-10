using Microsoft.AspNetCore.Mvc;
using Sakura.PT.DTOs;
using Sakura.PT.Mappers;
using Sakura.PT.Services;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly IWebHostEnvironment _env;

    public UsersController(IUserService userService, ILogger<UsersController> logger, IWebHostEnvironment env)
    {
        _userService = userService;
        _logger = logger;
        _env = env;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(UserForRegistrationDto userForRegistrationDto)
    {
        _logger.LogInformation("Register request received for user: {UserName}", userForRegistrationDto.UserName);
        try
        {
            var newUser = await _userService.RegisterAsync(userForRegistrationDto);
            var userDto = Mapper.ToUserPublicProfileDto(newUser);
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
    [AllowAnonymous]
    public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
    {
        _logger.LogInformation("Login request received for user: {UserName}", userForLoginDto.UserName);
        try
        {
            var loginResponse = await _userService.LoginAsync(userForLoginDto);
            _logger.LogInformation("User {UserName} logged in successfully.", userForLoginDto.UserName);

            Response.Cookies.Append("authToken", loginResponse.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7),
                Domain = "localhost"
            });
            
            var userProfile = Mapper.ToUserPrivateProfileDto(loginResponse.User);

            return Ok(new { User = userProfile });
        }
        catch (Exception ex)
        { 
            _logger.LogError(ex, "Login failed for user {UserName}: {ErrorMessage}", userForLoginDto.UserName, ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserPrivateProfileDto>> GetMyProfile()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized("Invalid user identifier.");
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        return Ok(Mapper.ToUserPrivateProfileDto(user));
    }

    [HttpGet("{id:int}")]
    [Authorize] // It's good practice to require auth to see user profiles
    public async Task<ActionResult<UserPublicProfileDto>> GetUser(int id)
    {
        _logger.LogInformation("GetUser request received for id: {Id}", id);
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound("User not found.");
        }
        return Ok(Mapper.ToUserPublicProfileDto(user));
    }

    [HttpGet("{userId}/badges")]
    [Authorize]
    public async Task<IActionResult> GetUserBadges(int userId)
    {
        var badges = await _userService.GetUserBadgesAsync(userId);
        return Ok(badges);
    }

    [HttpGet("me/badges")]
    [Authorize]
    public async Task<IActionResult> GetMyBadges()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized("Invalid user identifier.");
        }

        var badges = await _userService.GetUserBadgesAsync(userId);
        return Ok(badges);
    }
}