using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sakura.PT.Services;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator,Moderator")] // Only allow authorized staff to access
public class SakuraCoinController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<SakuraCoinController> _logger;

    public SakuraCoinController(IUserService userService, ILogger<SakuraCoinController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Adds or removes SakuraCoins for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user to modify.</param>
    /// <param name="amount">The amount of coins to add (can be negative to remove).</param>
    /// <returns>A confirmation message.</returns>
    [HttpPost("add")]
    public async Task<IActionResult> AddCoins(int userId, long amount)
    {
        _logger.LogInformation("Attempting to add {Amount} SakuraCoins to user {UserId} by {AdminUserName}", amount, userId, User.Identity.Name);
        var result = await _userService.AddSakuraCoinsAsync(userId, amount);

        if (!result)
        {
            _logger.LogWarning("Failed to add SakuraCoins to user {UserId}.", userId);
            return BadRequest("Failed to update SakuraCoins. User not found or insufficient balance.");
        }

        _logger.LogInformation("Successfully added {Amount} SakuraCoins to user {UserId}.", amount, userId);
        return Ok("SakuraCoins updated successfully.");
    }
}
