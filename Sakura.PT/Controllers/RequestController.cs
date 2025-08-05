using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sakura.PT.DTOs;
using Sakura.PT.Enums;
using Sakura.PT.Services;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RequestController : ControllerBase
{
    private readonly IRequestService _requestService;
    private readonly ILogger<RequestController> _logger;

    public RequestController(IRequestService requestService, ILogger<RequestController> logger)
    {
        _requestService = requestService;
        _logger = logger;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateRequestDto createRequestDto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message, request) = await _requestService.CreateRequestAsync(createRequestDto, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to create request: {Message}", message);
            return BadRequest(message);
        }

        return Ok(request);
    }

    [HttpPost("{requestId}/addBounty")]
    public async Task<IActionResult> AddBounty(int requestId, [FromForm] ulong amount)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _requestService.AddBountyAsync(requestId, amount, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to add bounty: {Message}", message);
            return BadRequest(message);
        }

        return Ok(new { message = message });
    }

    [HttpGet]
    public async Task<IActionResult> GetRequests([FromQuery] RequestStatus? status = RequestStatus.Pending)
    {
        var requests = await _requestService.GetRequestsAsync(status);
        return Ok(requests);
    }

    [HttpPost("{requestId}/fill/{torrentId}")]
    public async Task<IActionResult> FillRequest(int requestId, int torrentId)
    {
        var fillerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _requestService.FillRequestAsync(requestId, torrentId, fillerUserId);

        if (!success)
        {
            _logger.LogWarning("Failed to fill request: {Message}", message);
            return BadRequest(message);
        }

        return Ok(new { message = message });
    }
}
