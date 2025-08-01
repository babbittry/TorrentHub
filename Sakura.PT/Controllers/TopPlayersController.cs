using Microsoft.AspNetCore.Mvc;
using Sakura.PT.Enums;
using Sakura.PT.Services;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<IActionResult> GetTopPlayers(TopPlayerType type)
    {
        try
        {
            var topPlayers = await _topPlayersService.GetTopPlayersAsync(type);
            return Ok(topPlayers);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid TopPlayerType requested: {Type}", type);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting top players for type: {Type}", type);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPost("refreshCache")]
    // This endpoint should ideally be protected by admin/moderator roles
    // [Authorize(Roles = "Administrator,Moderator")]
    public async Task<IActionResult> RefreshCache()
    {
        _logger.LogInformation("Manual cache refresh requested for top players.");
        await _topPlayersService.RefreshTopPlayersCacheAsync();
        return Ok("Top players cache refreshed.");
    }
}
