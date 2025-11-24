using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Enums;
using TorrentHub.Core.Services;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommentController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim!);
    }

    /// <summary>
    /// 获取指定内容的评论列表（支持种子、求种、论坛主题）
    /// </summary>
    [HttpGet("{type}/{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CommentListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CommentListResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CommentListResponse>>> GetComments(
        string type,
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!Enum.TryParse<CommentableType>(type, true, out var commentableType))
        {
            return BadRequest(ApiResponse<CommentListResponse>.ErrorResult("无效的评论类型"));
        }

        if (pageSize > 100) pageSize = 100;

        // 将 page 转换为 afterFloor（懒加载参数）
        var afterFloor = (page - 1) * pageSize;
        var result = await _commentService.GetCommentsLazyAsync(commentableType, id, afterFloor, pageSize);
        return Ok(ApiResponse<CommentListResponse>.SuccessResult(result));
    }

    /// <summary>
    /// 发表新评论
    /// </summary>
    [HttpPost("{type}/{id}")]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CommentDto>>> PostComment(
        string type,
        int id,
        [FromBody] CreateCommentDto dto)
    {
        if (!Enum.TryParse<CommentableType>(type, true, out var commentableType))
        {
            return BadRequest(ApiResponse<CommentDto>.ErrorResult("无效的评论类型"));
        }

        var userId = GetCurrentUserId();
        var result = await _commentService.PostCommentAsync(commentableType, id, dto, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<CommentDto>.ErrorResult(result.Message));
        }

        return Ok(ApiResponse<CommentDto>.SuccessResult(result.Comment!, "评论发表成功"));
    }

    /// <summary>
    /// 获取单条评论详情
    /// </summary>
    [HttpGet("detail/{commentId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CommentDto>>> GetComment(int commentId)
    {
        var comment = await _commentService.GetCommentByIdAsync(commentId);
        
        if (comment == null)
        {
            return NotFound(ApiResponse<CommentDto>.ErrorResult("评论不存在"));
        }

        return Ok(ApiResponse<CommentDto>.SuccessResult(comment));
    }

    /// <summary>
    /// 更新评论内容
    /// </summary>
    [HttpPut("{commentId}")]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CommentDto>>> UpdateComment(
        int commentId,
        [FromBody] UpdateCommentDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _commentService.UpdateCommentAsync(commentId, dto, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<CommentDto>.ErrorResult(result.Message));
        }

        return Ok(ApiResponse<CommentDto>.SuccessResult(result.Comment!, "评论更新成功"));
    }

    /// <summary>
    /// 删除评论
    /// </summary>
    [HttpDelete("{commentId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteComment(int commentId)
    {
        var userId = GetCurrentUserId();
        var result = await _commentService.DeleteCommentAsync(commentId, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult("评论删除成功"));
    }

    /// <summary>
    /// 获取用户的评论历史
    /// </summary>
    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CommentListResponse>>> GetUserComments(
        int userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageSize > 100) pageSize = 100;

        var result = await _commentService.GetUserCommentsAsync(userId, page, pageSize);
        return Ok(ApiResponse<CommentListResponse>.SuccessResult(result));
    }
}