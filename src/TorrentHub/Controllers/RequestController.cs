using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Enums;
using TorrentHub.Mappers;
using TorrentHub.Services.Interfaces;

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
    [HttpPost]
    public async Task<ActionResult<ApiResponse<RequestDto>>> CreateRequest([FromBody] CreateRequestDto createRequestDto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message, request) = await _requestService.CreateRequestAsync(createRequestDto, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to create request: {Message}", message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = message
            });
        }

        return CreatedAtAction(nameof(GetRequestById), new { id = request!.Id }, 
            new ApiResponse<RequestDto>
            {
                Success = true,
                Data = Mapper.ToRequestDto(request),
                Message = "Request created successfully."
            });
    }

    /// <summary>
    /// Adds bounty (Coins) to an existing request.
    /// </summary>
    [HttpPatch("{requestId}/bounty")]
    public async Task<ActionResult<ApiResponse<object>>> AddBounty(int requestId, [FromBody] AddBountyRequestDto requestDto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _requestService.AddBountyAsync(requestId, requestDto, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to add bounty: {Message}", message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = message
        });
    }

    /// <summary>
    /// Gets a list of requests, filterable by status.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RequestDto>>>> GetRequests(
        [FromQuery] RequestStatus? status, 
        [FromQuery] string sortBy = "createdAt", 
        [FromQuery] string sortOrder = "desc")
    {
        var requests = await _requestService.GetRequestsAsync(status, sortBy, sortOrder);
        return Ok(new ApiResponse<List<RequestDto>>
        {
            Success = true,
            Data = requests.Select(r => Mapper.ToRequestDto(r)).ToList(),
            Message = "Requests retrieved successfully."
        });
    }

    /// <summary>
    /// Gets a single request by ID.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetRequestById")]
    public async Task<ActionResult<ApiResponse<RequestDto>>> GetRequestById(int id)
    {
        var request = await _requestService.GetRequestByIdAsync(id);
        if (request == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Request not found."
            });
        }
        return Ok(new ApiResponse<RequestDto>
        {
            Success = true,
            Data = Mapper.ToRequestDto(request),
            Message = "Request retrieved successfully."
        });
    }

    /// <summary>
    /// Fills a request with a specific torrent.
    /// </summary>
    [HttpPatch("{requestId}/fill")]
    public async Task<ActionResult<ApiResponse<object>>> FillRequest(int requestId, [FromBody] FillRequestDto requestDto)
    {
        var fillerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _requestService.FillRequestAsync(requestId, requestDto, fillerUserId);

        if (!success)
        {
            _logger.LogWarning("Failed to fill request: {Message}", message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = message
        });
    }

    /// <summary>
    /// Confirms that a torrent correctly fulfills the request.
    /// </summary>
    [HttpPost("{requestId}/confirm")]
    public async Task<ActionResult<ApiResponse<object>>> ConfirmFulfillment(int requestId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _requestService.ConfirmFulfillmentAsync(requestId, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to confirm request fulfillment: {Message}", message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = message
        });
    }

    /// <summary>
    /// Rejects a torrent fulfillment, providing a reason.
    /// </summary>
    [HttpPost("{requestId}/reject")]
    public async Task<ActionResult<ApiResponse<object>>> RejectFulfillment(int requestId, [FromBody] RejectFulfillmentDto rejectDto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _requestService.RejectFulfillmentAsync(requestId, rejectDto, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to reject request fulfillment: {Message}", message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = message
        });
    }
}
