
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TorrentHub.Core.DTOs;
using TorrentHub.Services;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/polls")]
[Authorize]
public class PollsController : ControllerBase
{
    private readonly IPollService _pollService;

    public PollsController(IPollService pollService)
    {
        _pollService = pollService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PollDto>>> GetAllPolls()
    {
        var userId = GetCurrentUserId();
        var polls = await _pollService.GetAllAsync(userId);
        return Ok(polls);
    }

    [HttpGet("latest")]
    public async Task<ActionResult<PollDto>> GetLatestPoll()
    {
        var userId = GetCurrentUserId();
        var poll = await _pollService.GetLatestActiveAsync(userId);
        if (poll == null)
        {
            return NotFound();
        }
        return Ok(poll);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<PollDto>> CreatePoll([FromBody] CreatePollDto dto)
    {
        var userId = GetCurrentUserId()!.Value; // Should not be null for admin
        var poll = await _pollService.CreateAsync(dto, userId);
        return CreatedAtAction(nameof(GetPollById), new { id = poll.Id }, poll);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PollDto>> GetPollById(int id)
    {
        var userId = GetCurrentUserId();
        var poll = await _pollService.GetByIdAsync(id, userId);
        if (poll == null)
        {
            return NotFound();
        }
        return Ok(poll);
    }

    [HttpPost("{id:int}/vote")]
    public async Task<IActionResult> Vote(int id, [FromBody] VoteDto dto)
    {
        var userId = GetCurrentUserId()!.Value; // Should not be null for voting
        var (success, message) = await _pollService.VoteAsync(id, dto, userId);
        if (!success)
        {
            return BadRequest(new { message });
        }
        return Ok();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeletePoll(int id)
    {
        var (success, message) = await _pollService.DeleteAsync(id);
        if (!success)
        {
            return NotFound(new { message });
        }
        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

