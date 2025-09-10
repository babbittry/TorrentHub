using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Enums;
using TorrentHub.Mappers;
using TorrentHub.Services;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Controllers;

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

    [HttpPatch("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileDto profileDto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized("Invalid user identifier.");
        }

        try
        {
            var updatedUser = await _userService.UpdateUserProfileAsync(userId, profileDto);
            return Ok(Mapper.ToUserPrivateProfileDto(updatedUser));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user profile for {UserId}: {ErrorMessage}", userId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("me/password")]
    [Authorize]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized("Invalid user identifier.");
        }

        try
        {
            await _userService.ChangePasswordAsync(userId, changePasswordDto);
            return Ok(new { message = "Password changed successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change password for {UserId}: {ErrorMessage}", userId, ex.Message);
            return BadRequest(new { message = ex.Message });
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

    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<ActionResult<IEnumerable<UserPublicProfileDto>>> GetUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        [FromQuery] string? searchTerm = null)
    {
        var users = await _userService.GetUsersAsync(page, pageSize, searchTerm);
        return Ok(users.Select(Mapper.ToUserPublicProfileDto));
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<IActionResult> UpdateUser(
        int id, 
        [FromBody] UpdateUserAdminDto updateUserAdminDto)
    {
        try
        {
            var updatedUser = await _userService.UpdateUserByAdminAsync(id, updateUserAdminDto);
            return Ok(Mapper.ToUserPublicProfileDto(updatedUser));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId} by admin: {ErrorMessage}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<UserPublicProfileDto>> GetUser(int id)
    {
        var userProfile = await _userService.GetUserPublicProfileAsync(id);
        if (userProfile == null)
        {
            return NotFound("User not found.");
        }
        return Ok(userProfile);
    }

    [HttpGet("{userId}/badges")]
    [Authorize]
    public async Task<ActionResult<List<BadgeDto>>> GetUserBadges(int userId)
    {
        var badges = await _userService.GetUserBadgesAsync(userId);
        return Ok(badges);
    }

    [HttpGet("me/badges")]
    [Authorize]
    public async Task<ActionResult<List<BadgeDto>>> GetMyBadges()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized("Invalid user identifier.");
        }

        var badges = await _userService.GetUserBadgesAsync(userId);
        return Ok(badges);
    }

    [HttpGet("{id:int}/uploads")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<TorrentDto>>> GetUserUploads(int id)
    {
        var uploads = await _userService.GetUserUploadsAsync(id);
        return Ok(uploads);
    }

    [HttpGet("{id:int}/peers")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<PeerDto>>> GetUserPeers(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId) || (currentUserId != id && !User.IsInRole(nameof(UserRole.Administrator))))
        {
            return Forbid();
        }

        var peers = await _userService.GetUserPeersAsync(id);
        return Ok(peers);
    }

    // --- 2FA Management ---

    [HttpPost("me/2fa/generate-setup")]
    [Authorize]
    public async Task<IActionResult> GenerateTwoFactorSetup()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized("Invalid user identifier.");
        }

        try
        {
            var (manualEntryKey, qrCodeImageUrl) = await _userService.GenerateTwoFactorSetupAsync(userId);
            return Ok(new { manualEntryKey, qrCodeImageUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate 2FA setup for user {UserId}", userId);
            return BadRequest(new { message = "Failed to generate 2FA setup." });
        }
    }

    [HttpPost("me/2fa/switch-to-app")]
    [Authorize]
    public async Task<IActionResult> SwitchToAuthenticatorApp([FromBody] TwoFactorVerificationRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized("Invalid user identifier.");
        }

        try
        {
            var success = await _userService.SwitchToAuthenticatorAppAsync(userId, request.Code);
            if (!success)
            {
                return BadRequest(new { message = "Invalid verification code." });
            }
            return Ok(new { message = "Two-factor authentication method switched to Authenticator App." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to App 2FA for user {UserId}", userId);
            return BadRequest(new { message = "Failed to switch 2FA method." });
        }
    }

    [HttpPost("me/2fa/switch-to-email")]
    [Authorize]
    public async Task<IActionResult> SwitchToEmail([FromBody] TwoFactorVerificationRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized("Invalid user identifier.");
        }

        try
        {
            var success = await _userService.SwitchToEmailAsync(userId, request.Code);
            if (!success)
            {
                return BadRequest(new { message = "Invalid verification code." });
            }
            return Ok(new { message = "Two-factor authentication method switched to Email." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to Email 2FA for user {UserId}", userId);
            return BadRequest(new { message = "Failed to switch 2FA method." });
        }
    }

    [HttpPost("me/equip-badge/{badgeId:int}")]
    [Authorize]
    public async Task<IActionResult> EquipBadge(int badgeId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized("Invalid user identifier.");
        }

        try
        {
            await _userService.EquipBadgeAsync(userId, badgeId);
            return Ok(new { message = "Badge equipped successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to equip badge {BadgeId} for user {UserId}", badgeId, userId);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("me/title")]
    [Authorize]
    public async Task<IActionResult> UpdateUserTitle([FromBody] UpdateUserTitleRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized("Invalid user identifier.");
        }

        try
        {
            await _userService.UpdateUserTitleAsync(userId, request.Title);
            return Ok(new { message = "User title updated successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user title for user {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
    }
}
