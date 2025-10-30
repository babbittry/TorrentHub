using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Enums;
using TorrentHub.Core.Services;
using TorrentHub.Mappers;

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
    public async Task<ActionResult<ApiResponse<string>>> GetOrCreateCredential(int torrentId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<string>.ErrorResult("Invalid user token"));
        }

        try
        {
            var credential = await _credentialService.GetOrCreateCredentialAsync(userId, torrentId);
            return Ok(ApiResponse<string>.SuccessResult(credential.ToString()));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Failed to create credential for user {UserId} and torrent {TorrentId}", userId, torrentId);
            return BadRequest(ApiResponse<string>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating credential for user {UserId} and torrent {TorrentId}", userId, torrentId);
            return StatusCode(500, ApiResponse<string>.ErrorResult("An error occurred while creating credential"));
        }
    }

    /// <summary>
    /// Get all credentials for the authenticated user with filtering, sorting and pagination
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<CredentialDto>>>> GetMyCredentials([FromQuery] CredentialFilterRequest filter)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<PaginatedResult<CredentialDto>>.ErrorResult("Invalid user token"));
        }

        try
        {
            var (credentials, totalCount) = await _credentialService.GetUserCredentialsAsync(userId, filter);
            var dtos = credentials.Select(Mapper.ToCredentialDto).ToList();
            
            var result = new PaginatedResult<CredentialDto>
            {
                Items = dtos,
                TotalItems = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
            
            return Ok(ApiResponse<PaginatedResult<CredentialDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credentials for user {UserId}", userId);
            return StatusCode(500, ApiResponse<PaginatedResult<CredentialDto>>.ErrorResult("An error occurred while retrieving credentials"));
        }
    }

    /// <summary>
    /// Revoke a specific credential (user can only revoke their own)
    /// </summary>
    [HttpPost("revoke/{credential}")]
    public async Task<ActionResult<ApiResponse>> RevokeCredential(Guid credential, [FromBody] RevokeCredentialRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.ErrorResult("Invalid user token"));
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
                return Ok(ApiResponse.SuccessResult("Credential revoked successfully"));
            }
            return NotFound(ApiResponse.ErrorResult("Credential not found or already revoked"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking credential {Credential}", credential);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while revoking credential"));
        }
    }

    /// <summary>
    /// Revoke all credentials for the authenticated user
    /// </summary>
    [HttpPost("revoke-all")]
    public async Task<ActionResult<ApiResponse<RevokeAllCredentialsResponse>>> RevokeAllCredentials([FromBody] RevokeCredentialRequest? request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<RevokeAllCredentialsResponse>.ErrorResult("Invalid user token"));
        }

        try
        {
            var (count, torrentIds) = await _credentialService.RevokeUserCredentialsAsync(userId, request?.Reason ?? "User bulk revoke");
            _logger.LogInformation("User {UserId} revoked all {Count} credentials", userId, count);
            var response = new RevokeAllCredentialsResponse
            {
                RevokedCount = count,
                AffectedTorrentIds = torrentIds,
                Message = $"Successfully revoked {count} credentials"
            };
            return Ok(ApiResponse<RevokeAllCredentialsResponse>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all credentials for user {UserId}", userId);
            return StatusCode(500, ApiResponse<RevokeAllCredentialsResponse>.ErrorResult("An error occurred while revoking credentials"));
        }
    }

    /// <summary>
    /// Revoke multiple specific credentials (user can only revoke their own)
    /// </summary>
    [HttpPost("revoke-batch")]
    public async Task<ActionResult<ApiResponse<RevokeAllCredentialsResponse>>> RevokeBatchCredentials([FromBody] RevokeBatchRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<RevokeAllCredentialsResponse>.ErrorResult("Invalid user token"));
        }

        if (request.CredentialIds == null || request.CredentialIds.Length == 0)
        {
            return BadRequest(ApiResponse<RevokeAllCredentialsResponse>.ErrorResult("CredentialIds cannot be empty"));
        }

        try
        {
            var (count, torrentIds) = await _credentialService.RevokeBatchAsync(userId, request.CredentialIds, request.Reason ?? "User batch revoke");
            _logger.LogInformation("User {UserId} revoked {Count} credentials in batch", userId, count);
            var response = new RevokeAllCredentialsResponse
            {
                RevokedCount = count,
                AffectedTorrentIds = torrentIds,
                Message = $"Successfully revoked {count} out of {request.CredentialIds.Length} credentials"
            };
            return Ok(ApiResponse<RevokeAllCredentialsResponse>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking batch credentials for user {UserId}", userId);
            return StatusCode(500, ApiResponse<RevokeAllCredentialsResponse>.ErrorResult("An error occurred while revoking credentials"));
        }
    }

    /// <summary>
    /// Admin: Get all credentials for a specific user with filtering, sorting and pagination
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<ActionResult<ApiResponse<PaginatedResult<CredentialDto>>>> GetUserCredentials(int userId, [FromQuery] CredentialFilterRequest filter)
    {
        try
        {
            var (credentials, totalCount) = await _credentialService.GetUserCredentialsAsync(userId, filter);
            var dtos = credentials.Select(Mapper.ToCredentialDto).ToList();
            
            var result = new PaginatedResult<CredentialDto>
            {
                Items = dtos,
                TotalItems = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
            
            return Ok(ApiResponse<PaginatedResult<CredentialDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credentials for user {UserId}", userId);
            return StatusCode(500, ApiResponse<PaginatedResult<CredentialDto>>.ErrorResult("An error occurred while retrieving credentials"));
        }
    }

    /// <summary>
    /// Admin: Revoke any credential
    /// </summary>
    [HttpPost("admin/revoke/{credential}")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<ActionResult<ApiResponse>> AdminRevokeCredential(Guid credential, [FromBody] RevokeCredentialRequest request)
    {
        try
        {
            var success = await _credentialService.RevokeCredentialAsync(credential, request.Reason ?? "Admin revoked");
            if (success)
            {
                _logger.LogInformation("Admin revoked credential {Credential}", credential);
                return Ok(ApiResponse.SuccessResult("Credential revoked successfully"));
            }
            return NotFound(ApiResponse.ErrorResult("Credential not found or already revoked"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking credential {Credential}", credential);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while revoking credential"));
        }
    }

    /// <summary>
    /// Admin: Revoke all credentials for a specific user
    /// </summary>
    [HttpPost("admin/revoke-user/{userId}")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<ActionResult<ApiResponse<RevokeAllCredentialsResponse>>> RevokeUserCredentials(int userId, [FromBody] RevokeCredentialRequest request)
    {
        try
        {
            var (count, torrentIds) = await _credentialService.RevokeUserCredentialsAsync(userId, request.Reason ?? "Admin bulk revoke");
            _logger.LogInformation("Admin revoked {Count} credentials for user {UserId}", count, userId);
            var response = new RevokeAllCredentialsResponse
            {
                RevokedCount = count,
                AffectedTorrentIds = torrentIds,
                Message = $"Revoked {count} credentials"
            };
            return Ok(ApiResponse<RevokeAllCredentialsResponse>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking credentials for user {UserId}", userId);
            return StatusCode(500, ApiResponse<RevokeAllCredentialsResponse>.ErrorResult("An error occurred while revoking credentials"));
        }
    }

    /// <summary>
    /// Admin: Revoke multiple specific credentials for any user
    /// </summary>
    [HttpPost("admin/revoke-batch")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<ActionResult<ApiResponse<RevokeAllCredentialsResponse>>> AdminRevokeBatchCredentials([FromBody] RevokeBatchRequest request)
    {
        if (request.CredentialIds == null || request.CredentialIds.Length == 0)
        {
            return BadRequest(ApiResponse<RevokeAllCredentialsResponse>.ErrorResult("CredentialIds cannot be empty"));
        }

        try
        {
            var (count, torrentIds) = await _credentialService.AdminRevokeBatchAsync(request.CredentialIds, request.Reason ?? "Admin batch revoke");
            _logger.LogInformation("Admin revoked {Count} credentials in batch", count);
            var response = new RevokeAllCredentialsResponse
            {
                RevokedCount = count,
                AffectedTorrentIds = torrentIds,
                Message = $"Successfully revoked {count} out of {request.CredentialIds.Length} credentials"
            };
            return Ok(ApiResponse<RevokeAllCredentialsResponse>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in admin batch credential revocation");
            return StatusCode(500, ApiResponse<RevokeAllCredentialsResponse>.ErrorResult("An error occurred while revoking credentials"));
        }
    }

    /// <summary>
    /// Admin: Trigger cleanup of inactive credentials
    /// </summary>
    [HttpPost("admin/cleanup")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<ActionResult<ApiResponse<int>>> CleanupInactiveCredentials([FromQuery] int inactiveDays = 90)
    {
        try
        {
            var count = await _credentialService.CleanupInactiveCredentialsAsync(inactiveDays);
            _logger.LogInformation("Cleaned up {Count} inactive credentials (inactive for {Days} days)", count, inactiveDays);
            return Ok(ApiResponse<int>.SuccessResult(count, $"Cleaned up {count} inactive credentials"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up inactive credentials");
            return StatusCode(500, ApiResponse<int>.ErrorResult("An error occurred during cleanup"));
        }
    }
}