using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TorrentHub.Core.DTOs;
using TorrentHub.Mappers;
using TorrentHub.Services;
using TorrentHub.Services.Interfaces;

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
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var items = await _storeService.GetAvailableItemsAsync(userId);
        return Ok(items);
    }

    [HttpPost("purchase")]
    public async Task<IActionResult> PurchaseItem([FromBody] PurchaseItemRequestDto request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        _logger.LogInformation("User {UserId} is attempting to purchase item {ItemId} with quantity {Quantity}.", userId, request.StoreItemId, request.Quantity);

        try
        {
            var result = await _storeService.PurchaseItemAsync(userId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Purchase failed for user {UserId} for item {ItemId}.", userId, request.StoreItemId);
            return BadRequest(new { message = ex.Message });
        }
    }
}
