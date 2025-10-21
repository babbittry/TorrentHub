using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TorrentHub.Core.DTOs;
using TorrentHub.Services;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/coins")]
[Authorize]
public class CoinController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<CoinController> _logger;

    public CoinController(IUserService userService, ILogger<CoinController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Transfers coins to another user.
    /// </summary>
    [HttpPost("transfer")]
    public async Task<IActionResult> TransferCoins([FromBody] TransferCoinsRequestDto dto)
    {
        var fromUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, message) = await _userService.TransferCoinsAsync(fromUserId, dto.ToUserId, dto.Amount, dto.Notes);

        if (!success)
        {
            _logger.LogWarning("Coin transfer failed from {FromUserId} to {ToUserId}. Reason: {Message}", fromUserId, dto.ToUserId, message);
            return BadRequest(new { message });
        }

        return Ok(new { message });
    }

    /// <summary>
    /// Tips coins to another user for their contribution.
    /// Only supports tipping for Torrents.
    /// </summary>
    [HttpPost("tip")]
    public async Task<IActionResult> TipCoins([FromBody] TipCoinsRequestDto dto)
    {
        // Only allow tipping for Torrents
        if (dto.ContextType != "Torrent")
        {
            _logger.LogWarning("Tip attempt for non-Torrent context: {ContextType} by user {UserId}", dto.ContextType, User.FindFirstValue(ClaimTypes.NameIdentifier));
            return BadRequest(new { message = "error.tip.invalidContext" });
        }

        var fromUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var notes = $"{dto.ContextType}:{dto.ContextId}"; // Combine context into notes
        var (success, message) = await _userService.TipCoinsAsync(fromUserId, dto.ToUserId, dto.Amount, notes);

        if (!success)
        {
            _logger.LogWarning("Coin tip failed from {FromUserId} to {ToUserId}. Reason: {Message}", fromUserId, dto.ToUserId, message);
            return BadRequest(new { message });
        }

        return Ok(new { message });
    }
}
