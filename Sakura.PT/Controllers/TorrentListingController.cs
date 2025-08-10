using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Sakura.PT.DTOs;
using Sakura.PT.Services;
using Sakura.PT.Mappers;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("api/torrents/listing")]
public class TorrentListingController : ControllerBase
{
    private readonly ITorrentListingService _torrentListingService;
    private readonly ILogger<TorrentListingController> _logger;

    public TorrentListingController(ITorrentListingService torrentListingService, ILogger<TorrentListingController> logger)
    {
        _torrentListingService = torrentListingService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<TorrentDto>>> GetTorrents([FromQuery] TorrentFilterDto filter)
    {
        if (filter.PageNumber < 1 || filter.PageSize < 1 || filter.PageSize > 100) // Basic validation
        {
            return BadRequest(new { message = "Invalid pagination parameters." });
        }

        try
        {
            var torrents = await _torrentListingService.GetTorrentsAsync(filter);
            return Ok(torrents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting torrents with filter: {Filter}", JsonSerializer.Serialize(filter));
            return StatusCode(500, new { message = "An unexpected error occurred." });
        }
    }
}