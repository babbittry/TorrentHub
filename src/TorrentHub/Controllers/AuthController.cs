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

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7),
            Path = "/" // Set path to root to be accessible site-wide
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
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
            await _userService.RegisterAsync(registrationDto);
            return Ok(new { message = "Registration successful. Please log in." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for user {UserName}: {ErrorMessage}", registrationDto.UserName, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _userService.LogoutAsync(refreshToken);
            Response.Cookies.Delete("refreshToken");
        }
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<RefreshTokenResponseDto>> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized("Refresh token not found.");
        }

        var result = await _userService.RefreshTokenAsync(refreshToken);
        if (result == null)
        {
            return Unauthorized("Invalid or expired refresh token.");
        }

        var (newAccessToken, user) = result.Value;
        var userProfile = Mapper.ToUserPrivateProfileDto(user);

        return Ok(new RefreshTokenResponseDto { AccessToken = newAccessToken, User = userProfile });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login(UserForLoginDto userForLoginDto)
    {
        _logger.LogInformation("Login request received for user: {UserName}", userForLoginDto.UserName);
        try
        {
            var (accessToken, refreshToken, user) = await _userService.LoginAsync(userForLoginDto);
            
            SetRefreshTokenCookie(refreshToken);
            
            var userProfile = Mapper.ToUserPrivateProfileDto(user);

            return Ok(new LoginResponseDto { AccessToken = accessToken, User = userProfile });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for user {UserName}: {ErrorMessage}", userForLoginDto.UserName, ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
    }
}

