
using System.Security.Claims;
using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sakura.PT.Data;
using Sakura.PT.Entities;
using Sakura.PT.Enums;
using Microsoft.Extensions.Logging;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("[controller]")]
public class TorrentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly BencodeParser _bencodeParser = new();
    private readonly ILogger<TorrentController> _logger;

    public TorrentController(ApplicationDbContext context, ILogger<TorrentController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("upload")]
    [Authorize]
    public async Task<IActionResult> Upload(IFormFile torrentFile, [FromForm] string? description, [FromForm] TorrentCategory category)
    {
        _logger.LogInformation("Upload request received for file: {FileName}, category: {Category}", torrentFile.FileName, category);

        if (torrentFile.Length == 0)
        {
            _logger.LogWarning("Upload failed: Torrent file is empty.");
            return BadRequest("Torrent file is empty.");
        }

        var torrent = await ParseTorrentFile(torrentFile);
        if (torrent == null)
        {
            _logger.LogWarning("Upload failed: Invalid torrent file format for {FileName}.", torrentFile.FileName);
            return BadRequest("Invalid torrent file.");
        }
        
        var infoHash = torrent.GetInfoHash();
        if (await _context.Torrents.AnyAsync(t => t.InfoHash == infoHash))
        {
            _logger.LogWarning("Upload failed: Torrent with infoHash {InfoHash} already exists.", infoHash);
            return BadRequest("Torrent already exists.");
        }

        try
        {
            var filePath = await SaveTorrentFile(torrentFile, infoHash);
            var torrentEntity = await CreateTorrentEntity(torrent, description, category, filePath);

            _context.Torrents.Add(torrentEntity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Torrent {TorrentName} (InfoHash: {InfoHash}) uploaded successfully by user {UserId}.", torrent.DisplayName, infoHash, User.FindFirstValue(ClaimTypes.NameIdentifier));
            return Ok(new { message = "Torrent uploaded successfully.", infoHash });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during torrent upload for file {FileName}.", torrentFile.FileName);
            return StatusCode(500, "An error occurred during upload.");
        }
    }

    [HttpPost("setFree/{torrentId}")]
    [Authorize(Roles = "Administrator")] // Only administrators can set torrents as free
    public async Task<IActionResult> SetFree(int torrentId, [FromQuery] DateTime? freeUntil)
    {
        _logger.LogInformation("SetFree request received for torrentId: {TorrentId}, freeUntil: {FreeUntil}", torrentId, freeUntil);
        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            _logger.LogWarning("SetFree failed: Torrent {TorrentId} not found.", torrentId);
            return NotFound("Torrent not found.");
        }

        torrent.IsFree = true;
        torrent.FreeUntil = freeUntil;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Torrent {TorrentName} (Id: {TorrentId}) set to free until {FreeUntil}.", torrent.Name, torrentId, freeUntil?.ToString() ?? "forever");

        return Ok(new { message = $"Torrent {torrent.Name} set to free until {freeUntil?.ToString() ?? "forever"}." });
    }

    [HttpPost("setSticky/{torrentId}")]
    [Authorize(Roles = "Administrator")] // Only administrators can set official sticky status
    public async Task<IActionResult> SetSticky(int torrentId, [FromQuery] TorrentStickyStatus status)
    {
        _logger.LogInformation("SetSticky request received for torrentId: {TorrentId}, status: {Status}", torrentId, status);
        if (status == TorrentStickyStatus.None)
        {
            _logger.LogWarning("SetSticky failed: Cannot set sticky status to None for torrent {TorrentId}.", torrentId);
            return BadRequest("Cannot set sticky status to None using this endpoint. Use a dedicated unsticky endpoint if needed.");
        }

        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            _logger.LogWarning("SetSticky failed: Torrent {TorrentId} not found.", torrentId);
            return NotFound("Torrent not found.");
        }

        torrent.StickyStatus = status;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Torrent {TorrentName} (Id: {TorrentId}) sticky status set to {Status}.", torrent.Name, torrentId, status);

        return Ok(new { message = $"Torrent {torrent.Name} sticky status set to {status}." });
    }

    private async Task<BencodeNET.Torrents.Torrent?> ParseTorrentFile(IFormFile file)
    {
        try
        {
            _logger.LogDebug("Parsing torrent file: {FileName}", file.FileName);
            await using var stream = file.OpenReadStream();
            return await _bencodeParser.ParseAsync<BencodeNET.Torrents.Torrent>(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing torrent file: {FileName}", file.FileName);
            return null;
        }
    }

    private async Task<string> SaveTorrentFile(IFormFile file, string infoHash)
    {
        var torrentsDir = Path.Combine(Directory.GetCurrentDirectory(), "torrents");
        if (!Directory.Exists(torrentsDir))
        {
            _logger.LogInformation("Creating torrents directory: {Directory}", torrentsDir);
            Directory.CreateDirectory(torrentsDir);
        }

        var filePath = Path.Combine(torrentsDir, $"{infoHash}.torrent");
        try
        {
            _logger.LogDebug("Saving torrent file to: {FilePath}", filePath);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving torrent file {FileName} to {FilePath}.", file.FileName, filePath);
            throw; // Re-throw to be caught by the Upload method's try-catch
        }
    }
    
    private async Task<Entities.Torrent> CreateTorrentEntity(BencodeNET.Torrents.Torrent torrent, string? description, TorrentCategory category, string filePath)
    {
        _logger.LogDebug("Creating torrent entity for {TorrentName} (InfoHash: {InfoHash}).", torrent.DisplayName, torrent.GetInfoHash());
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            _logger.LogError("User ID not found in claims during torrent entity creation.");
            throw new Exception("User ID not found");
        }
        var user = await _context.Users.FindAsync(int.Parse(userId));
        if (user == null)
        {
            _logger.LogError("User {UserId} not found in database during torrent entity creation.", userId);
            throw new Exception("User not found");
        } 
        
        return new Entities.Torrent
        {
            Name = torrent.DisplayName,
            InfoHash = torrent.GetInfoHash(),
            Description = description,
            Size = torrent.TotalSize,
            UploadedByUserId = user.Id,
            UploadedByUser = user,
            Category = category,
            CreatedAt = DateTime.UtcNow,
            IsFree = false,
            FreeUntil = null,
            StickyStatus = TorrentStickyStatus.None,
            FilePath = filePath
        };
    }

    [HttpGet("download/{torrentId}")]
    [Authorize]
    public async Task<IActionResult> Download(int torrentId)
    {
        _logger.LogInformation("Download request received for torrentId: {TorrentId}.", torrentId);
        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            _logger.LogWarning("Download failed: Torrent {TorrentId} not found.", torrentId);
            return NotFound("Torrent not found.");
        }

        var filePath = torrent.FilePath;
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogError("Download failed: Torrent file not found on server for torrent {TorrentId} at path {FilePath}.", torrentId, filePath);
            return NotFound("Torrent file not found on server.");
        }

        try
        {
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            _logger.LogInformation("Serving torrent file {FileName} for torrent {TorrentId}.", torrent.Name + ".torrent", torrentId);
            return File(fileStream, "application/x-bittorrent", torrent.Name + ".torrent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving torrent file {FileName} for torrent {TorrentId}.", torrent.Name + ".torrent", torrentId);
            return StatusCode(500, "An error occurred during file download.");
        }
    }
}

