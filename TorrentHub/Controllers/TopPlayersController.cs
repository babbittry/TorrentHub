using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TorrentHub.DTOs;
using TorrentHub.Enums;
using TorrentHub.Services;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/top-players")]
public class TopPlayersController : ControllerBase
{
    private readonly ITopPlayersService _topPlayersService;
    private readonly ILogger<TopPlayersController> _logger;

    public TopPlayersController(ITopPlayersService topPlayersService, ILogger<TopPlayersController> logger)
    {
        _topPlayersService = topPlayersService;
        _logger = logger;
    }

    [HttpGet("{type}")]
    public async Task<ActionResult<List<UserPublicProfileDto>>> GetTopPlayers(TopPlayerType type)
    {
        try
        {
            var topPlayers = await _topPlayersService.GetTopPlayersAsync(type);
            return Ok(topPlayers);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid TopPlayerType requested: {Type}", type);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting top players for type: {Type}", type);
            return StatusCode(500, new { message = "An unexpected error occurred." });
        }
    }

    [HttpPost("refresh-cache")]
    [Authorize(Roles = "Administrator,Moderator")]
    public async Task<IActionResult> RefreshCache()
    {
        _logger.LogInformation("Manual cache refresh requested for top players.");
        await _topPlayersService.RefreshTopPlayersCacheAsync();
        return Ok(new { message = "Top players cache refreshed." });
    }
}