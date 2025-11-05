using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TorrentHub.Core.DTOs;
using TorrentHub.Mappers;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class RequestCommentsController : ControllerBase
{
    private readonly IRequestCommentService _requestCommentService;
    private readonly ILogger<RequestCommentsController> _logger;

    public RequestCommentsController(IRequestCommentService requestCommentService, ILogger<RequestCommentsController> logger)
    {
        _requestCommentService = requestCommentService;
        _logger = logger;
    }

    [HttpPost("requests/{requestId}/comments")]
    public async Task<ActionResult<RequestCommentDto>> PostComment(int requestId, [FromBody] CreateRequestCommentRequestDto request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message, comment) = await _requestCommentService.PostCommentAsync(requestId, request, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to post comment: {Message}", message);
            return BadRequest(new { message = message });
        }

        return Ok(Mapper.ToRequestCommentDto(comment!));
    }

    /// <summary>
    /// Get comments with lazy loading
    /// </summary>
    [HttpGet("requests/{requestId}/comments")]
    public async Task<ActionResult<RequestCommentListResponse>> GetCommentsLazy(
        int requestId,
        [FromQuery] int afterFloor = 0,
        [FromQuery] int limit = 30)
    {
        var result = await _requestCommentService.GetCommentsLazyAsync(requestId, afterFloor, limit);
        return Ok(result);
    }

    [HttpPut("comments/{id:int}")]
    public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateRequestCommentRequestDto request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _requestCommentService.UpdateCommentAsync(id, request, userId);
        if (!success)
        {
            return BadRequest(new { message = message });
        }
        return Ok(new { message = message });
    }

    [HttpDelete("comments/{id:int}")]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _requestCommentService.DeleteCommentAsync(id, userId);
        if (!success)
        {
            return BadRequest(new { message = message });
        }
        return Ok(new { message = message });
    }
}