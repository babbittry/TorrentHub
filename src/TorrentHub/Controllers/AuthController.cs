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
            Path = "/"
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] UserForRegistrationDto registrationDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _userService.RegisterAsync(registrationDto);
            return Ok(new { message = "Registration successful. Please check your email to verify your account." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for user {UserName}: {ErrorMessage}", registrationDto.UserName, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("A verification token is required.");
        }

        var success = await _userService.VerifyEmailAsync(token);

        if (success)
        {
            // Redirect to a frontend page that says "Email verified, you can now log in"
            // Or just return a success message
            return Ok(new { message = "Email verified successfully. You can now log in." });
        }

        return BadRequest("Invalid or expired email verification token.");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResponseDto>> Login(UserForLoginDto userForLoginDto)
    {
        _logger.LogInformation("Login request received for user: {UserNameOrEmail}", userForLoginDto.UserNameOrEmail);
        
        var (loginResultDto, refreshToken) = await _userService.LoginAsync(userForLoginDto);

        switch (loginResultDto.Result)
        {
            case Core.Enums.LoginResultType.Success:
                if (refreshToken != null)
                {
                    SetRefreshTokenCookie(refreshToken);
                }
                return Ok(loginResultDto);

            case Core.Enums.LoginResultType.RequiresTwoFactor:
                return Ok(loginResultDto);

            case Core.Enums.LoginResultType.InvalidCredentials:
                _logger.LogWarning("Login failed for user {UserNameOrEmail}: Invalid credentials.", userForLoginDto.UserNameOrEmail);
                return Unauthorized(new { message = "Invalid username or password." });

            case Core.Enums.LoginResultType.EmailNotVerified:
                _logger.LogWarning("Login failed for user {UserNameOrEmail}: Email not verified.", userForLoginDto.UserNameOrEmail);
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Please verify your email address before logging in." });

            case Core.Enums.LoginResultType.Banned:
                _logger.LogWarning("Login failed for user {UserNameOrEmail}: Account is banned.", userForLoginDto.UserNameOrEmail);
                return Unauthorized(new { message = "Your account has been banned." });

            default:
                _logger.LogError("An unexpected login result was encountered for user {UserNameOrEmail}.", userForLoginDto.UserNameOrEmail);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
        }
    }

    [HttpPost("login-2fa")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResponseDto>> Login2fa([FromBody] UserForLogin2faDto login2faDto)
    {
        try
        {
            var (accessToken, refreshToken, user) = await _userService.Login2faAsync(login2faDto);
            
            SetRefreshTokenCookie(refreshToken);
            
            var userProfile = Mapper.ToUserPrivateProfileDto(user);
            userProfile.UnreadMessagesCount = await _userService.GetUnreadMessagesCountAsync(user.Id);

            return Ok(new LoginResponseDto { AccessToken = accessToken, User = userProfile });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "2FA Login failed for user {UserName}: {ErrorMessage}", login2faDto.UserName, ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("send-email-code")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendEmailVerificationCode([FromBody] SendEmailCodeRequestDto request)
    {
        try
        {
            await _userService.SendLoginVerificationEmailAsync(request.UserName);
            return Ok(new { message = "If a matching account exists, a verification code has been sent to the associated email address." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification code for {UserName}", request.UserName);
            return Ok(new { message = "If a matching account exists, a verification code has been sent to the associated email address." });
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
    [ProducesResponseType(typeof(RefreshTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
        userProfile.UnreadMessagesCount = await _userService.GetUnreadMessagesCountAsync(user.Id);

        return Ok(new RefreshTokenResponseDto { AccessToken = newAccessToken, User = userProfile });
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendVerificationRequestDto request)
    {
        var result = await _userService.ResendVerificationEmailAsync(request.UserNameOrEmail);
        if (result)
        {
            return Ok(new { message = "If an account with that username or email exists and is not verified, a new verification link has been sent to the associated email address." });
        }
        
        return BadRequest(new { message = "This account has already been verified." });
    }
}
