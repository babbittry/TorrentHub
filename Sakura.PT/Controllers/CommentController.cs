using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sakura.PT.Entities;
using Sakura.PT.Services;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("api/torrents/{torrentId}/[controller]")]
[Authorize]
public class CommentController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly ILogger<CommentController> _logger;

    public CommentController(ICommentService commentService, ILogger<CommentController> logger)
    {
        _commentService = commentService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> PostComment(int torrentId, [FromBody] Comment newComment)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message, comment) = await _commentService.PostCommentAsync(torrentId, newComment.Text, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to post comment: {Message}", message);
            return BadRequest(message);
        }

        return Ok(comment);
    }
}
