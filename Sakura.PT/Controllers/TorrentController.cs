
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sakura.PT.Enums;
using Sakura.PT.Services;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("[controller]")]
public class TorrentController : ControllerBase
{
    private readonly ITorrentService _torrentService;
    private readonly ILogger<TorrentController> _logger;

    public TorrentController(ITorrentService torrentService, ILogger<TorrentController> logger)
    {
        _torrentService = torrentService;
        _logger = logger;
    }

    [HttpPost("upload")]
    [Authorize]
    public async Task<IActionResult> Upload(IFormFile torrentFile, [FromForm] string? description, [FromForm] TorrentCategory category)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var (success, message, infoHash) = await _torrentService.UploadTorrentAsync(torrentFile, description, category, userId);

        if (!success)
        {
            _logger.LogWarning("Torrent upload failed: {Message}", message);
            return BadRequest(message);
        }

        _logger.LogInformation("Torrent uploaded successfully. InfoHash: {InfoHash}", infoHash);
        return Ok(new { message = message, infoHash = infoHash });
    }

    [HttpPost("setFree/{torrentId}")]
    [Authorize(Roles = "Administrator")] // Only administrators can set torrents as free
    public async Task<IActionResult> SetFree(int torrentId, [FromQuery] DateTime? freeUntil)
    {
        var (success, message) = await _torrentService.SetFreeAsync(torrentId, freeUntil);
        if (!success)
        {
            _logger.LogWarning("SetFree failed: {Message}", message);
            return NotFound(message);
        }
        return Ok(new { message = message });
    }

    [HttpPost("setSticky/{torrentId}")]
    [Authorize(Roles = "Administrator")] // Only administrators can set official sticky status
    public async Task<IActionResult> SetSticky(int torrentId, [FromQuery] TorrentStickyStatus status)
    {
        var (success, message) = await _torrentService.SetStickyAsync(torrentId, status);
        if (!success)
        {
            _logger.LogWarning("SetSticky failed: {Message}", message);
            return BadRequest(message);
        }
        return Ok(new { message = message });
    }

    [HttpGet("download/{torrentId}")]
    [Authorize]
    public async Task<IActionResult> Download(int torrentId)
    {
        var fileStreamResult = await _torrentService.DownloadTorrentAsync(torrentId);
        if (fileStreamResult == null)
        {
            return NotFound("Torrent file not found or an error occurred.");
        }
        return fileStreamResult;
    }

    [HttpPost("{torrentId}/completeInfo")]
    [Authorize]
    public async Task<IActionResult> CompleteInfo(int torrentId, [FromForm] string imdbId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var (success, message) = await _torrentService.CompleteTorrentInfoAsync(torrentId, imdbId, userId);
        if (!success)
        {
            _logger.LogWarning("CompleteInfo failed: {Message}", message);
            return BadRequest(message);
        }
        return Ok(new { message = message });
    }

    [HttpPost("{torrentId}/applyFreeleechToken")]
    [Authorize]
    public async Task<IActionResult> ApplyFreeleechToken(int torrentId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var (success, message) = await _torrentService.ApplyFreeleechTokenAsync(torrentId, userId);
        if (!success)
        {
            _logger.LogWarning("ApplyFreeleechToken failed: {Message}", message);
            return BadRequest(message);
        }
        return Ok(new { message = message });
    }
}

