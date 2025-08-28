using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TorrentHub.Mappers;
using TorrentHub.Core.DTOs;
using TorrentHub.Services;

namespace TorrentHub.Controllers;

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
    public async Task<ActionResult<PaginatedResult<TorrentDto>>> Search([FromQuery] TorrentFilterDto filter)
    {
        var result = await _torrentListingService.GetTorrentsAsync(filter);
        return Ok(result);
    }
}
