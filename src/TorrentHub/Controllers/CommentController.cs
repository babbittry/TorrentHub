using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TorrentHub.Core.DTOs;
using TorrentHub.Mappers;
using TorrentHub.Services;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api")] // Changed route to handle /api/comments/{id} directly
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(ICommentService commentService, ILogger<CommentsController> logger)
    {
        _commentService = commentService;
        _logger = logger;
    }

    [HttpPost("torrents/{torrentId}/comments")]
    public async Task<ActionResult<CommentDto>> PostComment(int torrentId, [FromBody] CreateCommentRequestDto request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message, comment) = await _commentService.PostCommentAsync(torrentId, request, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to post comment: {Message}", message);
            return BadRequest(new { message = message });
        }

        return Ok(Mapper.ToCommentDto(comment!));
    }

    [HttpGet("torrents/{torrentId}/comments")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsForTorrent(
        int torrentId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        var comments = await _commentService.GetCommentsForTorrentAsync(torrentId, page, pageSize);
        return Ok(comments.Select(Mapper.ToCommentDto));
    }

    [HttpPut("comments/{id:int}")]
    public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentRequestDto request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _commentService.UpdateCommentAsync(id, request, userId);
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
        var (success, message) = await _commentService.DeleteCommentAsync(id, userId);
        if (!success)
        {
            return BadRequest(new { message = message });
        }
        return Ok(new { message = message });
    }
}

