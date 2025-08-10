using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sakura.PT.DTOs;
using Sakura.PT.Mappers;
using Sakura.PT.Services;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("api/torrents/{torrentId}/comments")]
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

    [HttpPost]
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
}