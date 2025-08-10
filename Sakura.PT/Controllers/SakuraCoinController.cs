using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sakura.PT.Services;
using Sakura.PT.DTOs;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("api/sakuracoins")]
[Authorize(Roles = "Administrator,Moderator")] // Only allow authorized staff to access
public class SakuraCoinsController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<SakuraCoinsController> _logger;

    public SakuraCoinsController(IUserService userService, ILogger<SakuraCoinsController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Adds or removes SakuraCoins for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user to modify.</param>
    /// <param name="request">The DTO containing the amount of coins to add (can be negative to remove).</param>
    /// <returns>A confirmation message.</returns>
    [HttpPatch("{userId}")]
    public async Task<IActionResult> UpdateCoins(int userId, [FromBody] UpdateSakuraCoinsRequestDto request)
    {
        _logger.LogInformation("Attempting to update {Amount} SakuraCoins for user {UserId} by {AdminUserName}", request.Amount, userId, User.Identity?.Name ?? "UnknownAdmin");
        var result = await _userService.AddSakuraCoinsAsync(userId, request);

        if (!result)
        {
            _logger.LogWarning("Failed to update SakuraCoins for user {UserId}.", userId);
            return BadRequest(new { message = "Failed to update SakuraCoins. User not found or insufficient balance." });
        }

        _logger.LogInformation("Successfully updated {Amount} SakuraCoins for user {UserId}.", request.Amount, userId);
        return Ok(new { message = "SakuraCoins updated successfully." });
    }
}