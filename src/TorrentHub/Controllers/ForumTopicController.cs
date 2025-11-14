using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Services;
using System.Security.Claims;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/forum")]
[Authorize]
public class ForumTopicController : ControllerBase
{
    private readonly IForumTopicService _forumTopicService;

    public ForumTopicController(IForumTopicService forumTopicService)
    {
        _forumTopicService = forumTopicService;
    }
    
    private int GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }
        return int.Parse(userIdClaim);
    }

    /// <summary>
    /// 获取论坛分类列表
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<ForumCategoryDto>>>> GetCategories()
    {
        var categories = await _forumTopicService.GetCategoriesAsync();
        return Ok(ApiResponse<List<ForumCategoryDto>>.SuccessResult(categories));
    }

    /// <summary>
    /// 获取指定分类下的主题列表
    /// </summary>
    [HttpGet("topics")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ForumTopicDto>>>> GetTopics(
        [FromQuery] int categoryId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var topics = await _forumTopicService.GetTopicsAsync(categoryId, page, pageSize);
        return Ok(ApiResponse<PaginatedResult<ForumTopicDto>>.SuccessResult(topics));
    }

    /// <summary>
    /// 获取主题详情(包含评论列表)
    /// </summary>
    /// <remarks>
    /// 注意: 评论管理现在由统一的 CommentController 处理
    /// - 获取评论: GET /api/comment/forumtopic/{topicId}
    /// - 发表评论: POST /api/comment/forumtopic/{topicId}
    /// - 更新评论: PUT /api/comment/{commentId}
    /// - 删除评论: DELETE /api/comment/{commentId}
    /// </remarks>
    [HttpGet("topics/{topicId}")]
    public async Task<ActionResult<ApiResponse<ForumTopicDetailDto>>> GetTopicById(
        int topicId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var topic = await _forumTopicService.GetTopicByIdAsync(topicId, page, pageSize);
            return Ok(ApiResponse<ForumTopicDetailDto>.SuccessResult(topic));
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
    }

    /// <summary>
    /// 创建新主题(包含初始帖子)
    /// </summary>
    [HttpPost("topics")]
    [ProducesResponseType(typeof(ApiResponse<ForumTopicDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ForumTopicDetailDto>>> CreateTopic([FromBody] CreateForumTopicDto createTopicDto)
    {
        try
        {
            var authorId = GetUserId();
            var topic = await _forumTopicService.CreateTopicAsync(createTopicDto, authorId);
            return CreatedAtAction(nameof(GetTopicById), new { topicId = topic.Id }, 
                ApiResponse<ForumTopicDetailDto>.SuccessResult(topic, "Forum topic created successfully."));
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
        catch (UnauthorizedAccessException e)
        {
            return StatusCode(403, new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
    }

    /// <summary>
    /// 更新主题标题
    /// </summary>
    [HttpPut("topics/{topicId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateTopic(int topicId, [FromBody] UpdateForumTopicDto updateTopicDto)
    {
        try
        {
            var userId = GetUserId();
            await _forumTopicService.UpdateTopicAsync(topicId, updateTopicDto, userId);
            return Ok(ApiResponse.SuccessResult("Forum topic updated successfully."));
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
        catch (UnauthorizedAccessException e)
        {
            return StatusCode(403, new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
    }

    /// <summary>
    /// 删除主题
    /// </summary>
    [HttpDelete("topics/{topicId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteTopic(int topicId)
    {
        try
        {
            var userId = GetUserId();
            await _forumTopicService.DeleteTopicAsync(topicId, userId);
            return Ok(ApiResponse.SuccessResult("Forum topic deleted successfully."));
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
        catch (UnauthorizedAccessException e)
        {
            return StatusCode(403, new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
    }

    /// <summary>
    /// 锁定主题(禁止发表新评论)
    /// </summary>
    [HttpPatch("topics/{topicId}/lock")]
    [Authorize(Roles = "Administrator,Moderator")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> LockTopic(int topicId)
    {
        try
        {
            await _forumTopicService.LockTopicAsync(topicId);
            return Ok(ApiResponse.SuccessResult("Forum topic locked successfully."));
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
    }
    
    /// <summary>
    /// 解锁主题
    /// </summary>
    [HttpPatch("topics/{topicId}/unlock")]
    [Authorize(Roles = "Administrator,Moderator")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> UnlockTopic(int topicId)
    {
        try
        {
            await _forumTopicService.UnlockTopicAsync(topicId);
            return Ok(ApiResponse.SuccessResult("Forum topic unlocked successfully."));
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
    }

    /// <summary>
    /// 置顶主题
    /// </summary>
    [HttpPatch("topics/{topicId}/sticky")]
    [Authorize(Roles = "Administrator,Moderator")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> PinTopic(int topicId)
    {
        try
        {
            await _forumTopicService.PinTopicAsync(topicId);
            return Ok(ApiResponse.SuccessResult("Forum topic pinned successfully."));
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
    }
    
    /// <summary>
    /// 取消置顶主题
    /// </summary>
    [HttpPatch("topics/{topicId}/unsticky")]
    [Authorize(Roles = "Administrator,Moderator")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> UnpinTopic(int topicId)
    {
        try
        {
            await _forumTopicService.UnpinTopicAsync(topicId);
            return Ok(ApiResponse.SuccessResult("Forum topic unpinned successfully."));
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
    }
}