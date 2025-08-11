using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Sakura.PT.Services;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly IStatsService _statsService;

    public StatsController(IStatsService statsService)
    {
        _statsService = statsService;
    }

    [HttpGet]
    [AllowAnonymous] // Stats can be public
    public async Task<IActionResult> GetSiteStats()
    {
        var stats = await _statsService.GetSiteStatsAsync();
        return Ok(stats);
    }
}
