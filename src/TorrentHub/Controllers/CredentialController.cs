using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TorrentHub.Core.Enums;
using TorrentHub.Core.Services;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CredentialController : ControllerBase
{
    private readonly ITorrentCredentialService _credentialService;
    private readonly ILogger<CredentialController> _logger;

    public CredentialController(ITorrentCredentialService credentialService, ILogger<CredentialController> logger)
    {
        _credentialService = credentialService;
        _logger = logger;
    }

    /// <summary>
    /// Get or create a credential for the authenticated user and specified torrent
    /// </summary>
    [HttpGet("torrent/{torrentId}")]
    public async Task<IActionResult> GetOrCreateCredential(int torrentId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        try
        {
            var credential = await _credentialService.GetOrCreateCredentialAsync(userId, torrentId);
            return Ok(new { credential = credential.ToString() });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Failed to create credential for user {UserId} and torrent {TorrentId}", userId, torrentId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating credential for user {UserId} and torrent {TorrentId}", userId, torrentId);
            return StatusCode(500, new { message = "An error occurred while creating credential" });
        }
    }

    /// <summary>
    /// Get all credentials for the authenticated user
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyCredentials([FromQuery] bool includeRevoked = false)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        try
        {
            var credentials = await _credentialService.GetUserCredentialsAsync(userId, includeRevoked);
            return Ok(credentials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credentials for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving credentials" });
        }
    }

    /// <summary>
    /// Revoke a specific credential (user can only revoke their own)
    /// </summary>
    [HttpPost("revoke/{credential}")]
    public async Task<IActionResult> RevokeCredential(Guid credential, [FromBody] RevokeCredentialRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        try
        {
            // Validate that the credential belongs to the user
            var (isValid, credentialUserId, _) = await _credentialService.ValidateCredentialAsync(credential);
            if (!isValid || credentialUserId != userId)
            {
                return Forbid();
            }

            var success = await _credentialService.RevokeCredentialAsync(credential, request.Reason ?? "User revoked");
            if (success)
            {
                return Ok(new { message = "Credential revoked successfully" });
            }
            return NotFound(new { message = "Credential not found or already revoked" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking credential {Credential}", credential);
            return StatusCode(500, new { message = "An error occurred while revoking credential" });
        }
    }

    /// <summary>
    /// Revoke all credentials for the authenticated user
    /// </summary>
    [HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAllCredentials([FromBody] RevokeCredentialRequest? request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        try
        {
            var count = await _credentialService.RevokeUserCredentialsAsync(userId, request?.Reason ?? "User bulk revoke");
            _logger.LogInformation("User {UserId} revoked all {Count} credentials", userId, count);
            return Ok(new { message = $"Successfully revoked {count} credentials", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all credentials for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while revoking credentials" });
        }
    }

    /// <summary>
    /// Admin: Get all credentials for a specific user
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<IActionResult> GetUserCredentials(int userId, [FromQuery] bool includeRevoked = false)
    {
        try
        {
            var credentials = await _credentialService.GetUserCredentialsAsync(userId, includeRevoked);
            return Ok(credentials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credentials for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving credentials" });
        }
    }

    /// <summary>
    /// Admin: Revoke any credential
    /// </summary>
    [HttpPost("admin/revoke/{credential}")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<IActionResult> AdminRevokeCredential(Guid credential, [FromBody] RevokeCredentialRequest request)
    {
        try
        {
            var success = await _credentialService.RevokeCredentialAsync(credential, request.Reason ?? "Admin revoked");
            if (success)
            {
                _logger.LogInformation("Admin revoked credential {Credential}", credential);
                return Ok(new { message = "Credential revoked successfully" });
            }
            return NotFound(new { message = "Credential not found or already revoked" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking credential {Credential}", credential);
            return StatusCode(500, new { message = "An error occurred while revoking credential" });
        }
    }

    /// <summary>
    /// Admin: Revoke all credentials for a specific user
    /// </summary>
    [HttpPost("admin/revoke-user/{userId}")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<IActionResult> RevokeUserCredentials(int userId, [FromBody] RevokeCredentialRequest request)
    {
        try
        {
            var count = await _credentialService.RevokeUserCredentialsAsync(userId, request.Reason ?? "Admin bulk revoke");
            _logger.LogInformation("Admin revoked {Count} credentials for user {UserId}", count, userId);
            return Ok(new { message = $"Revoked {count} credentials", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking credentials for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while revoking credentials" });
        }
    }

    /// <summary>
    /// Admin: Trigger cleanup of inactive credentials
    /// </summary>
    [HttpPost("admin/cleanup")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<IActionResult> CleanupInactiveCredentials([FromQuery] int inactiveDays = 90)
    {
        try
        {
            var count = await _credentialService.CleanupInactiveCredentialsAsync(inactiveDays);
            _logger.LogInformation("Cleaned up {Count} inactive credentials (inactive for {Days} days)", count, inactiveDays);
            return Ok(new { message = $"Cleaned up {count} inactive credentials", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up inactive credentials");
            return StatusCode(500, new { message = "An error occurred during cleanup" });
        }
    }
}

public class RevokeCredentialRequest
{
    public string? Reason { get; set; }
}