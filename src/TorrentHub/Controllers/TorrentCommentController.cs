using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TorrentHub.Core.DTOs;
using TorrentHub.Mappers;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api")] // Changed route to handle /api/comments/{id} directly
[Authorize]
public class TorrentCommentsController : ControllerBase
{
    private readonly ITorrentCommentService _torrentCommentService;
    private readonly ILogger<TorrentCommentsController> _logger;

    public TorrentCommentsController(ITorrentCommentService torrentCommentService, ILogger<TorrentCommentsController> logger)
    {
        _torrentCommentService = torrentCommentService;
        _logger = logger;
    }

    [HttpPost("torrents/{torrentId}/comments")]
    public async Task<ActionResult<TorrentCommentDto>> PostComment(int torrentId, [FromBody] CreateTorrentCommentRequestDto request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message, comment) = await _torrentCommentService.PostCommentAsync(torrentId, request, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to post comment: {Message}", message);
            return BadRequest(new { message = message });
        }

        return Ok(Mapper.ToTorrentCommentDto(comment!));
    }

    /// <summary>
    /// Get comments with lazy loading
    /// </summary>
    [HttpGet("torrents/{torrentId}/comments")]
    public async Task<ActionResult<TorrentCommentListResponse>> GetCommentsLazy(
        int torrentId,
        [FromQuery] int afterFloor = 0,
        [FromQuery] int limit = 30)
    {
        var result = await _torrentCommentService.GetCommentsLazyAsync(torrentId, afterFloor, limit);
        return Ok(result);
    }

    [HttpPut("comments/{id:int}")]
    public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateTorrentCommentRequestDto request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _torrentCommentService.UpdateCommentAsync(id, request, userId);
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
        var (success, message) = await _torrentCommentService.DeleteCommentAsync(id, userId);
        if (!success)
        {
            return BadRequest(new { message = message });
        }
        return Ok(new { message = message });
    }
}

