using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Enums;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ReactionController : ControllerBase
{
    private readonly IReactionService _reactionService;
    private readonly ILogger<ReactionController> _logger;

    public ReactionController(IReactionService reactionService, ILogger<ReactionController> logger)
    {
        _reactionService = reactionService;
        _logger = logger;
    }

    /// <summary>
    /// Add a reaction to a comment
    /// </summary>
    [HttpPost("{commentType}/{commentId:int}/reactions")]
    public async Task<IActionResult> AddReaction(
        string commentType,
        int commentId,
        [FromBody] AddReactionRequestDto request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, message) = await _reactionService.AddReactionAsync(
            commentType, 
            commentId, 
            request.Type, 
            userId);

        if (!success)
        {
            _logger.LogWarning("Failed to add reaction: {Message}", message);
            return BadRequest(new { message });
        }

        return Ok(new { message });
    }

    /// <summary>
    /// Remove a reaction from a comment
    /// </summary>
    [HttpDelete("{commentType}/{commentId:int}/reactions/{type}")]
    public async Task<IActionResult> RemoveReaction(
        string commentType,
        int commentId,
        ReactionType type)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, message) = await _reactionService.RemoveReactionAsync(
            commentType, 
            commentId, 
            type, 
            userId);

        if (!success)
        {
            _logger.LogWarning("Failed to remove reaction: {Message}", message);
            return BadRequest(new { message });
        }

        return Ok(new { message });
    }

    /// <summary>
    /// Get all reactions for a comment
    /// </summary>
    [HttpGet("{commentType}/{commentId:int}/reactions")]
    public async Task<ActionResult<CommentReactionsDto>> GetReactions(
        string commentType,
        int commentId)
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) 
            ? (int?)id 
            : null;

        var reactions = await _reactionService.GetReactionsAsync(commentType, commentId, userId);
        return Ok(reactions);
    }

    /// <summary>
    /// Get reactions for multiple comments in batch
    /// </summary>
    [HttpPost("{commentType}/reactions/batch")]
    public async Task<ActionResult<Dictionary<int, CommentReactionsDto>>> GetReactionsBatch(
        string commentType,
        [FromBody] GetReactionsBatchRequestDto request)
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) 
            ? (int?)id 
            : null;

        var reactions = await _reactionService.GetReactionsBatchAsync(
            commentType, 
            request.CommentIds, 
            userId);

        return Ok(reactions);
    }
}