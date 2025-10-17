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
    public async Task<ActionResult<List<ForumCategoryDto>>> GetCategories()
    {
        var categories = await _forumService.GetCategoriesAsync();
        return Ok(categories);
    }

    // GET /api/forum/topics?categoryId=1
    [HttpGet("topics")]
    public async Task<ActionResult<PaginatedResult<ForumTopicDto>>> GetTopics([FromQuery] int categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var topics = await _forumService.GetTopicsAsync(categoryId, page, pageSize);
        return Ok(topics);
    }

    // GET /api/forum/topics/{topicId}
    [HttpGet("topics/{topicId}")]
    public async Task<ActionResult<ForumTopicDetailDto>> GetTopicById(int topicId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var topic = await _forumService.GetTopicByIdAsync(topicId, page, pageSize);
            return Ok(topic);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    // POST /api/forum/topics
    [HttpPost("topics")]
    public async Task<ActionResult<ForumTopicDetailDto>> CreateTopic([FromBody] CreateForumTopicDto createTopicDto)
    {
        var authorId = GetUserId();
        var topic = await _forumService.CreateTopicAsync(createTopicDto, authorId);
        return CreatedAtAction(nameof(GetTopicById), new { topicId = topic.Id }, topic);
    }

    // POST /api/forum/topics/{topicId}/posts
    [HttpPost("topics/{topicId}/posts")]
    public async Task<ActionResult<ForumPostDto>> CreatePost(int topicId, [FromBody] CreateForumPostDto createPostDto)
    {
        try
        {
            var authorId = GetUserId();
            var post = await _forumService.CreatePostAsync(topicId, createPostDto, authorId);
            return Ok(post);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    /// Get posts with lazy loading
    /// </summary>
    [HttpGet("topics/{topicId}/posts")]
    public async Task<ActionResult<PaginatedResult<ForumPostDto>>> GetPosts(
        int topicId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        try
        {
            var result = await _forumService.GetPostsAsync(topicId, page, pageSize);
            return Ok(result);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    // PUT /api/forum/topics/{topicId}
    [HttpPut("topics/{topicId}")]
    public async Task<IActionResult> UpdateTopic(int topicId, [FromBody] UpdateForumTopicDto updateTopicDto)
    {
        try
        {
            var userId = GetUserId();
            await _forumService.UpdateTopicAsync(topicId, updateTopicDto, userId);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // PUT /api/forum/posts/{postId}
    [HttpPut("posts/{postId}")]
    public async Task<IActionResult> UpdatePost(int postId, [FromBody] UpdateForumPostDto updatePostDto)
    {
        try
        {
            var userId = GetUserId();
            await _forumService.UpdatePostAsync(postId, updatePostDto, userId);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // DELETE /api/forum/topics/{topicId}
    [HttpDelete("topics/{topicId}")]
    public async Task<IActionResult> DeleteTopic(int topicId)
    {
        try
        {
            var userId = GetUserId();
            await _forumService.DeleteTopicAsync(topicId, userId);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // DELETE /api/forum/posts/{postId}
    [HttpDelete("posts/{postId}")]
    public async Task<IActionResult> DeletePost(int postId)
    {
        try
        {
            var userId = GetUserId();
            await _forumService.DeletePostAsync(postId, userId);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }

    // PATCH /api/forum/topics/{topicId}/lock
    [HttpPatch("topics/{topicId}/lock")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> LockTopic(int topicId)
    {
        try
        {
            await _forumService.LockTopicAsync(topicId);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
    // PATCH /api/forum/topics/{topicId}/unlock
    [HttpPatch("topics/{topicId}/unlock")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> UnlockTopic(int topicId)
    {
        try
        {
            await _forumService.UnlockTopicAsync(topicId);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    // PATCH /api/forum/topics/{topicId}/sticky
    [HttpPatch("topics/{topicId}/sticky")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> PinTopic(int topicId)
    {
        try
        {
            await _forumService.PinTopicAsync(topicId);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
    // PATCH /api/forum/topics/{topicId}/unsticky
    [HttpPatch("topics/{topicId}/unsticky")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> UnpinTopic(int topicId)
    {
        try
        {
            await _forumService.UnpinTopicAsync(topicId);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}

