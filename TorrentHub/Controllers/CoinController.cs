using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TorrentHub.DTOs;
using TorrentHub.Services;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/coins")]
[Authorize(Roles = "Administrator,Moderator")] // Only allow authorized staff to access
public class CoinsController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<CoinsController> _logger;

    public CoinsController(IUserService userService, ILogger<CoinsController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Adds or removes Coins for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user to modify.</param>
    /// <param name="request">The DTO containing the amount of coins to add (can be negative to remove).</param>
    /// <returns>A confirmation message.</returns>
    [HttpPatch("{userId}")]
    public async Task<IActionResult> UpdateCoins(int userId, [FromBody] UpdateCoinsRequestDto request)
    {
        _logger.LogInformation("Attempting to update {Amount} Coins for user {UserId} by {AdminUserName}", request.Amount, userId, User.Identity?.Name ?? "UnknownAdmin");
        var result = await _userService.AddCoinsAsync(userId, request);

        if (!result)
        {
            _logger.LogWarning("Failed to update Coins for user {UserId}.", userId);
            return BadRequest(new { message = "Failed to update Coins. User not found or insufficient balance." });
        }

        _logger.LogInformation("Successfully updated {Amount} Coins for user {UserId}.", request.Amount, userId);
        return Ok(new { message = "Coins updated successfully." });
    }
}