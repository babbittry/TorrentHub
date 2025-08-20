using Microsoft.AspNetCore.Mvc;
using TorrentHub.DTOs;
using TorrentHub.Services;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly IStatsService _statsService;

    public StatsController(IStatsService statsService)
    {
        _statsService = statsService;
    }

    [HttpGet]
    public async Task<ActionResult<SiteStatsDto>> GetSiteStats()
    {
        var stats = await _statsService.GetSiteStatsAsync();
        return Ok(stats);
    }
}