using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TorrentHub.Core.DTOs;
using TorrentHub.Mappers;
using TorrentHub.Services;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _env;

    public AuthController(IUserService userService, ILogger<AuthController> logger, IWebHostEnvironment env)
    {
        _userService = userService;
        _logger = logger;
        _env = env;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] UserForRegistrationDto registrationDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _userService.RegisterAsync(registrationDto);

            // Auto-login after registration
            var loginResponse = await _userService.LoginAsync(new UserForLoginDto { UserName = registrationDto.UserName, Password = registrationDto.Password });

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
            _logger.LogError(ex, "Registration failed for user {UserName}: {ErrorMessage}", registrationDto.UserName, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("authToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict,
            Domain = "localhost"
        });
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshToken()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized("Invalid user identifier.");
        }

        try
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            var loginResponse = await _userService.LoginAsync(new UserForLoginDto { UserName = user.UserName, Password = "" }); // Password is not used for refresh, but LoginAsync requires it.

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
            _logger.LogError(ex, "Failed to refresh token for user {UserId}: {ErrorMessage}", userId, ex.Message);
            return Unauthorized(new { message = ex.Message });
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
}

