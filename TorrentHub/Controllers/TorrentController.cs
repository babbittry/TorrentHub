using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TorrentHub.Enums;
using TorrentHub.DTOs;
using TorrentHub.Mappers;
using TorrentHub.Services;

namespace TorrentHub.Controllers;

[ApiController]
[Route("api/torrents")]
public class TorrentsController : ControllerBase
{
    private readonly ITorrentService _torrentService;
    private readonly ILogger<TorrentsController> _logger;

    public TorrentsController(ITorrentService torrentService, ILogger<TorrentsController> logger)
    {
        _torrentService = torrentService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Upload(IFormFile torrentFile, [FromForm] UploadTorrentRequestDto request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message, infoHash) = await _torrentService.UploadTorrentAsync(torrentFile, request, userId);

        if (!success)
        {
            _logger.LogWarning("Torrent upload failed: {Message}", message);
            return BadRequest(new { message = message });
        }

        _logger.LogInformation("Torrent uploaded successfully. InfoHash: {InfoHash}", infoHash);
        return Ok(new { message = message, infoHash = infoHash });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TorrentDto>> GetTorrent(int id)
    {
        var torrent = await _torrentService.GetTorrentByIdAsync(id);
        if (torrent == null)
        {
            return NotFound("Torrent not found.");
        }
        return Ok(Mapper.ToTorrentDto(torrent));
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteTorrent(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _torrentService.DeleteTorrentAsync(id, userId);
        if (!success)
        {
            return BadRequest(new { message = message });
        }
        return Ok(new { message = message });
    }

    [HttpPatch("{torrentId}/free")]
    [Authorize(Roles = "Administrator")] // Only administrators can set torrents as free
    public async Task<IActionResult> SetFree(int torrentId, [FromBody] DateTime freeUntil)
    {
        var (success, message) = await _torrentService.SetFreeAsync(torrentId, freeUntil);
        if (!success)
        {
            _logger.LogWarning("SetFree failed: {Message}", message);
            return NotFound(new { message = message });
        }
        return Ok(new { message = message });
    }

    [HttpPatch("{torrentId}/sticky")]
    [Authorize(Roles = "Administrator")] // Only administrators can set official sticky status
    public async Task<IActionResult> SetSticky(int torrentId, [FromBody] SetStickyRequestDto request)
    {
        var (success, message) = await _torrentService.SetStickyAsync(torrentId, request);
        if (!success)
        {
            _logger.LogWarning("SetSticky failed: {Message}", message);
            return BadRequest(new { message = message });
        }
        return Ok(new { message = message });
    }

    [HttpGet("{torrentId}/download")]
    [Authorize]
    public async Task<IActionResult> Download(int torrentId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var fileStreamResult = await _torrentService.DownloadTorrentAsync(torrentId, userId);
        if (fileStreamResult == null)
        {
            return NotFound("Torrent file not found or you do not have permission to download it.");
        }
        return fileStreamResult;
    }

    [HttpPatch("{torrentId}/info")]
    [Authorize]
    public async Task<IActionResult> CompleteInfo(int torrentId, [FromBody] CompleteInfoRequestDto request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _torrentService.CompleteTorrentInfoAsync(torrentId, request, userId);
        if (!success)
        {
            _logger.LogWarning("CompleteInfo failed: {Message}", message);
            return BadRequest(new { message = message });
        }
        return Ok(new { message = message });
    }

    [HttpPatch("{torrentId}/freeleech")]
    [Authorize]
    public async Task<IActionResult> ApplyFreeleech(int torrentId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found."));
        var (success, message) = await _torrentService.ApplyFreeleechAsync(torrentId, userId);
        if (!success)
        {
            _logger.LogWarning("ApplyFreeleech failed: {Message}", message);
            return BadRequest(new { message = message });
        }
        return Ok(new { message = message });
    }
}