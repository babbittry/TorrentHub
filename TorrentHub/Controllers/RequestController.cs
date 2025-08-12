using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TorrentHub.DTOs;
using TorrentHub.Enums;
using TorrentHub.Mappers;
using TorrentHub.Services;

namespace TorrentHub.Controllers;

/// <summary>
/// API Controller for managing user requests (bounties).
/// All endpoints require user authentication.
/// </summary>
[ApiController]
[Route("api/requests")]
[Authorize]
public class RequestsController : ControllerBase
{
    private readonly IRequestService _requestService;
    private readonly ILogger<RequestsController> _logger;

    public RequestsController(IRequestService requestService, ILogger<RequestsController> logger)
    {
        _requestService = requestService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new request (bounty).
    /// </summary>
    /// <param name="createRequestDto">The DTO containing the title, description, and initial bounty amount for the request.</param>
    /// <returns>The created request object if successful.</returns>
    [HttpPost]
    public async Task<ActionResult<RequestDto>> CreateRequest([FromBody] CreateRequestDto createRequestDto)
    {
        // 从 JWT Token 中获取当前登录用户的 ID
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message, request) = await _requestService.CreateRequestAsync(createRequestDto, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to create request: {Message}", message);
            return BadRequest(new { message = message });
        }

        return CreatedAtAction(nameof(GetRequestById), new { id = request!.Id }, Mapper.ToRequestDto(request));
    }

    /// <summary>
    /// Adds bounty (Coins) to an existing request.
    /// </summary>
    /// <param name="requestId">The ID of the request to add bounty to.</param>
    /// <param name="requestDto">The DTO containing the amount of Coins to add.</param>
    /// <returns>A success message.</returns>
    [HttpPatch("{requestId}/bounty")]
    public async Task<IActionResult> AddBounty(int requestId, [FromBody] AddBountyRequestDto requestDto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _requestService.AddBountyAsync(requestId, requestDto, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to add bounty: {Message}", message);
            return BadRequest(new { message = message });
        }

        return Ok(new { message = message });
    }

    /// <summary>
    /// Gets a list of requests, filterable by status.
    /// </summary>
    /// <param name="status">The status to filter by (Pending, Filled, Expired). Defaults to Pending.</param>
    /// <returns>A list of request objects.</returns>
    [HttpGet]
    public async Task<ActionResult<List<RequestDto>>> GetRequests([FromQuery] RequestStatus? status)
    {
        // 如果调用方没有提供 status，则默认为 Pending
        var requests = await _requestService.GetRequestsAsync(status ?? RequestStatus.Pending);
        return Ok(requests.Select(r => Mapper.ToRequestDto(r)).ToList());
    }

    /// <summary>
    /// Gets a single request by ID.
    /// </summary>
    /// <param name="id">The ID of the request.</param>
    /// <returns>The request object if found.</returns>
    [HttpGet("{id:int}", Name = "GetRequestById")]
    public async Task<ActionResult<RequestDto>> GetRequestById(int id)
    {
        var request = await _requestService.GetRequestByIdAsync(id); // Assuming a GetRequestByIdAsync method exists or can be added
        if (request == null)
        {
            return NotFound(new { message = "Request not found." });
        }
        return Ok(Mapper.ToRequestDto(request));
    }

    /// <summary>
    /// Fills a request with a specific torrent.
    /// </summary>
    /// <param name="requestId">The ID of the request to fill.</param>
    /// <param name="requestDto">The DTO containing the ID of the torrent that fulfills the request.</param>
    /// <returns>A success message.</returns>
    [HttpPatch("{requestId}/fill")]
    public async Task<IActionResult> FillRequest(int requestId, [FromBody] FillRequestDto requestDto)
    {
        var fillerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _requestService.FillRequestAsync(requestId, requestDto, fillerUserId);

        if (!success)
        {
            _logger.LogWarning("Failed to fill request: {Message}", message);
            return BadRequest(new { message = message });
        }

        return Ok(new { message = message });
    }
}