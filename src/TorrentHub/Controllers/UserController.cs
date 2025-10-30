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
    public async Task<ActionResult<ApiResponse<UserPrivateProfileDto>>> UpdateMyProfile([FromBody] UpdateUserProfileDto profileDto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid user identifier."
            });
        }

        try
        {
            var updatedUser = await _userService.UpdateUserProfileAsync(userId, profileDto);
            return Ok(new ApiResponse<UserPrivateProfileDto>
            {
                Success = true,
                Data = Mapper.ToUserPrivateProfileDto(updatedUser),
                Message = "Profile updated successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user profile for {UserId}: {ErrorMessage}", userId, ex.Message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpPost("me/password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangeMyPassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid user identifier."
            });
        }

        try
        {
            await _userService.ChangePasswordAsync(userId, changePasswordDto);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Password changed successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change password for {UserId}: {ErrorMessage}", userId, ex.Message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserPrivateProfileDto>>> GetMyProfile()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid user identifier."
            });
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "User not found."
            });
        }

        var userProfile = Mapper.ToUserPrivateProfileDto(user);
        userProfile.UnreadMessagesCount = await _userService.GetUnreadMessagesCountAsync(userId);
        
        return Ok(new ApiResponse<UserPrivateProfileDto>
        {
            Success = true,
            Data = userProfile,
            Message = "User profile retrieved successfully."
        });
    }

    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserPublicProfileDto>>>> GetUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        [FromQuery] string? searchTerm = null)
    {
        var users = await _userService.GetUsersAsync(page, pageSize, searchTerm);
        return Ok(new ApiResponse<IEnumerable<UserPublicProfileDto>>
        {
            Success = true,
            Data = users.Select(Mapper.ToUserPublicProfileDto),
            Message = "Users retrieved successfully."
        });
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<ActionResult<ApiResponse<UserPublicProfileDto>>> UpdateUser(
        int id, 
        [FromBody] UpdateUserAdminDto updateUserAdminDto)
    {
        try
        {
            var updatedUser = await _userService.UpdateUserByAdminAsync(id, updateUserAdminDto);
            return Ok(new ApiResponse<UserPublicProfileDto>
            {
                Success = true,
                Data = Mapper.ToUserPublicProfileDto(updatedUser),
                Message = "User updated successfully by administrator."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId} by admin: {ErrorMessage}", id, ex.Message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserPublicProfileDto>>> GetUser(int id)
    {
        var userProfile = await _userService.GetUserPublicProfileAsync(id);
        if (userProfile == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "User not found."
            });
        }
        return Ok(new ApiResponse<UserPublicProfileDto>
        {
            Success = true,
            Data = userProfile,
            Message = "User profile retrieved successfully."
        });
    }

    [HttpGet("{userId}/badges")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<BadgeDto>>>> GetUserBadges(int userId)
    {
        var badges = await _userService.GetUserBadgesAsync(userId);
        return Ok(new ApiResponse<List<BadgeDto>>
        {
            Success = true,
            Data = badges,
            Message = "User badges retrieved successfully."
        });
    }

    [HttpGet("me/badges")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<BadgeDto>>>> GetMyBadges()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid user identifier."
            });
        }

        var badges = await _userService.GetUserBadgesAsync(userId);
        return Ok(new ApiResponse<List<BadgeDto>>
        {
            Success = true,
            Data = badges,
            Message = "Your badges retrieved successfully."
        });
    }

    [HttpGet("{id:int}/uploads")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IEnumerable<TorrentDto>>>> GetUserUploads(int id)
    {
        var uploads = await _userService.GetUserUploadsAsync(id);
        return Ok(new ApiResponse<IEnumerable<TorrentDto>>
        {
            Success = true,
            Data = uploads,
            Message = "User uploads retrieved successfully."
        });
    }

    [HttpGet("{id:int}/peers")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IEnumerable<PeerDto>>>> GetUserPeers(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId) || (currentUserId != id && !User.IsInRole(nameof(UserRole.Administrator))))
        {
            return Forbid();
        }

        var peers = await _userService.GetUserPeersAsync(id);
        return Ok(new ApiResponse<IEnumerable<PeerDto>>
        {
            Success = true,
            Data = peers,
            Message = "User peers retrieved successfully."
        });
    }

    // --- 2FA Management ---

    [HttpPost("me/2fa/generate-setup")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TwoFactorSetupDto>>> GenerateTwoFactorSetup()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid user identifier."
            });
        }

        try
        {
            var (manualEntryKey, qrCodeImageUrl) = await _userService.GenerateTwoFactorSetupAsync(userId);
            return Ok(new ApiResponse<TwoFactorSetupDto>
            {
                Success = true,
                Data = new TwoFactorSetupDto
                {
                    ManualEntryKey = manualEntryKey,
                    QrCodeImageUrl = qrCodeImageUrl
                },
                Message = "2FA setup generated successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate 2FA setup for user {UserId}", userId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to generate 2FA setup."
            });
        }
    }

    [HttpPost("me/2fa/switch-to-app")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> SwitchToAuthenticatorApp([FromBody] TwoFactorVerificationRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid user identifier."
            });
        }

        try
        {
            var success = await _userService.SwitchToAuthenticatorAppAsync(userId, request.Code);
            if (!success)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid verification code."
                });
            }
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Two-factor authentication method switched to Authenticator App."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to App 2FA for user {UserId}", userId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to switch 2FA method."
            });
        }
    }

    [HttpPost("me/2fa/switch-to-email")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> SwitchToEmail([FromBody] TwoFactorVerificationRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid user identifier."
            });
        }

        try
        {
            var success = await _userService.SwitchToEmailAsync(userId, request.Code);
            if (!success)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid verification code."
                });
            }
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Two-factor authentication method switched to Email."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to Email 2FA for user {UserId}", userId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to switch 2FA method."
            });
        }
    }

    [HttpPost("me/equip-badge/{badgeId:int}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> EquipBadge(int badgeId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid user identifier."
            });
        }

        try
        {
            await _userService.EquipBadgeAsync(userId, badgeId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Badge equipped successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to equip badge {BadgeId} for user {UserId}", badgeId, userId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpPut("me/title")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> UpdateUserTitle([FromBody] UpdateUserTitleRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid user identifier."
            });
        }

        try
        {
            await _userService.UpdateUserTitleAsync(userId, request.Title);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "User title updated successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user title for user {UserId}", userId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }
}

// Helper DTO for 2FA setup response
public class TwoFactorSetupDto
{
    public required string ManualEntryKey { get; set; }
    public required string QrCodeImageUrl { get; set; }
}
