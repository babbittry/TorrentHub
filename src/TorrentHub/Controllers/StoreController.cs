using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TorrentHub.Core.DTOs;
using TorrentHub.Mappers;
using TorrentHub.Services;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/store")]
[Authorize]
public class StoreController : ControllerBase
{
    private readonly IStoreService _storeService;
    private readonly ILogger<StoreController> _logger;

    public StoreController(IStoreService storeService, ILogger<StoreController> logger)
    {
        _storeService = storeService;
        _logger = logger;
    }

    [HttpGet("items")]
    public async Task<ActionResult<List<StoreItemDto>>> GetStoreItems()
    {
        var items = await _storeService.GetAvailableItemsAsync();
        return Ok(items);
    }

    [HttpPost("items/{itemId}/purchase")]
    public async Task<IActionResult> PurchaseItem(int itemId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        _logger.LogInformation("User {UserId} is attempting to purchase item {ItemId}.", userId, itemId);

        var success = await _storeService.PurchaseItemAsync(userId, itemId);

        if (success)
        {
            return Ok(new { message = "Purchase successful!" });
        }

        return BadRequest(new { message = "Purchase failed. Please check your balance or try again later." });
    }
}
