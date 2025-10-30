using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TorrentHub.Core.DTOs;
using TorrentHub.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/forum")]
[Authorize]
public class ForumController : ControllerBase
{
    private readonly IForumService _forumService;

    public ForumController(IForumService forumService)
    {
        _forumService = forumService;
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

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<ForumCategoryDto>>>> GetCategories()
    {
        var categories = await _forumService.GetCategoriesAsync();
        return Ok(new ApiResponse<List<ForumCategoryDto>>
        {
            Success = true,
            Data = categories,
            Message = "Forum categories retrieved successfully."
        });
    }

    [HttpGet("topics")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ForumTopicDto>>>> GetTopics(
        [FromQuery] int categoryId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var topics = await _forumService.GetTopicsAsync(categoryId, page, pageSize);
        return Ok(new ApiResponse<PaginatedResult<ForumTopicDto>>
        {
            Success = true,
            Data = topics,
            Message = "Forum topics retrieved successfully."
        });
    }

    [HttpGet("topics/{topicId}")]
    public async Task<ActionResult<ApiResponse<ForumTopicDetailDto>>> GetTopicById(
        int topicId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var topic = await _forumService.GetTopicByIdAsync(topicId, page, pageSize);
            return Ok(new ApiResponse<ForumTopicDetailDto>
            {
                Success = true,
                Data = topic,
                Message = "Forum topic retrieved successfully."
            });
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

    [HttpPost("topics")]
    public async Task<ActionResult<ApiResponse<ForumTopicDetailDto>>> CreateTopic([FromBody] CreateForumTopicDto createTopicDto)
    {
        var authorId = GetUserId();
        var topic = await _forumService.CreateTopicAsync(createTopicDto, authorId);
        return CreatedAtAction(nameof(GetTopicById), new { topicId = topic.Id }, 
            new ApiResponse<ForumTopicDetailDto>
            {
                Success = true,
                Data = topic,
                Message = "Forum topic created successfully."
            });
    }

    [HttpPost("topics/{topicId}/posts")]
    public async Task<ActionResult<ApiResponse<ForumPostDto>>> CreatePost(int topicId, [FromBody] CreateForumPostDto createPostDto)
    {
        try
        {
            var authorId = GetUserId();
            var post = await _forumService.CreatePostAsync(topicId, createPostDto, authorId);
            return Ok(new ApiResponse<ForumPostDto>
            {
                Success = true,
                Data = post,
                Message = "Forum post created successfully."
            });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new ApiResponse<object>
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

    [HttpGet("topics/{topicId}/posts")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ForumPostDto>>>> GetPosts(
        int topicId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        try
        {
            var result = await _forumService.GetPostsAsync(topicId, page, pageSize);
            return Ok(new ApiResponse<PaginatedResult<ForumPostDto>>
            {
                Success = true,
                Data = result,
                Message = "Forum posts retrieved successfully."
            });
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

    [HttpPut("topics/{topicId}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateTopic(int topicId, [FromBody] UpdateForumTopicDto updateTopicDto)
    {
        try
        {
            var userId = GetUserId();
            await _forumService.UpdateTopicAsync(topicId, updateTopicDto, userId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Forum topic updated successfully."
            });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("posts/{postId}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdatePost(int postId, [FromBody] UpdateForumPostDto updatePostDto)
    {
        try
        {
            var userId = GetUserId();
            await _forumService.UpdatePostAsync(postId, updatePostDto, userId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Forum post updated successfully."
            });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("topics/{topicId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteTopic(int topicId)
    {
        try
        {
            var userId = GetUserId();
            await _forumService.DeleteTopicAsync(topicId, userId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Forum topic deleted successfully."
            });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("posts/{postId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeletePost(int postId)
    {
        try
        {
            var userId = GetUserId();
            await _forumService.DeletePostAsync(postId, userId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Forum post deleted successfully."
            });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = e.Message
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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

    [HttpPatch("topics/{topicId}/lock")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ApiResponse<object>>> LockTopic(int topicId)
    {
        try
        {
            await _forumService.LockTopicAsync(topicId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Forum topic locked successfully."
            });
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
    
    [HttpPatch("topics/{topicId}/unlock")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ApiResponse<object>>> UnlockTopic(int topicId)
    {
        try
        {
            await _forumService.UnlockTopicAsync(topicId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Forum topic unlocked successfully."
            });
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

    [HttpPatch("topics/{topicId}/sticky")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ApiResponse<object>>> PinTopic(int topicId)
    {
        try
        {
            await _forumService.PinTopicAsync(topicId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Forum topic pinned successfully."
            });
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
    
    [HttpPatch("topics/{topicId}/unsticky")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ApiResponse<object>>> UnpinTopic(int topicId)
    {
        try
        {
            await _forumService.UnpinTopicAsync(topicId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Forum topic unpinned successfully."
            });
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
