using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Enums;
using TorrentHub.Services.Interfaces;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/reactions")]
[Authorize]
public class ReactionController : ControllerBase
{
    private readonly IReactionService _reactionService;

    public ReactionController(IReactionService reactionService)
    {
        _reactionService = reactionService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim!);
    }

    /// <summary>
    /// 获取评论的表情回应列表
    /// </summary>
    [HttpGet("comment/{commentId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CommentReactionsDto>>> GetCommentReactions(int commentId)
    {
        var reactions = await _reactionService.GetReactionsAsync("Comment", commentId, GetCurrentUserId());
        return Ok(ApiResponse<CommentReactionsDto>.SuccessResult(reactions));
    }

    /// <summary>
    /// 添加表情回应
    /// </summary>
    [HttpPost("comment/{commentId}")]
    public async Task<ActionResult<ApiResponse<object>>> AddCommentReaction(
        int commentId,
        [FromBody] AddReactionRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _reactionService.AddReactionAsync("Comment", commentId, request.Type, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult("Reaction added successfully"));
    }

    /// <summary>
    /// 移除表情回应
    /// </summary>
    [HttpDelete("comment/{commentId}/{type}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveCommentReaction(
        int commentId,
        ReactionType type)
    {
        var userId = GetCurrentUserId();
        var result = await _reactionService.RemoveReactionAsync("Comment", commentId, type, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult("Reaction removed successfully"));
    }

    /// <summary>
    /// 批量获取表情回应
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<ApiResponse<Dictionary<int, CommentReactionsDto>>>> GetReactionsBatch(
        [FromBody] GetReactionsBatchRequestDto request)
    {
        var userId = GetCurrentUserId();
        var reactions = await _reactionService.GetReactionsBatchAsync("Comment", request.CommentIds, userId);
        return Ok(ApiResponse<Dictionary<int, CommentReactionsDto>>.SuccessResult(reactions));
    }
}