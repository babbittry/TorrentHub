using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sakura.PT.Data;
using Sakura.PT.Entities;
using Sakura.PT.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Sakura.PT.Services;

public class TorrentService : ITorrentService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly SakuraCoinSettings _sakuraCoinSettings;
    private readonly ILogger<TorrentService> _logger;
    private readonly BencodeParser _bencodeParser = new();

    public TorrentService(ApplicationDbContext context, IUserService userService, IOptions<SakuraCoinSettings> sakuraCoinSettings, ILogger<TorrentService> logger)
    {
        _context = context;
        _userService = userService;
        _sakuraCoinSettings = sakuraCoinSettings.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, string? InfoHash)> UploadTorrentAsync(IFormFile torrentFile, string? description, TorrentCategory category, int userId)
    {
        _logger.LogInformation("Upload request received for file: {FileName}, category: {Category}, user: {UserId}", torrentFile.FileName, category, userId);

        if (torrentFile.Length == 0)
        {
            return (false, "Torrent file is empty.", null);
        }

        var torrent = await ParseTorrentFile(torrentFile);
        if (torrent == null)
        {
            return (false, "Invalid torrent file.", null);
        }

        var infoHash = torrent.GetInfoHash();
        if (await _context.Torrents.AnyAsync(t => t.InfoHash == infoHash))
        {
            return (false, "Torrent already exists.", null);
        }

        try
        {
            var filePath = await SaveTorrentFile(torrentFile, infoHash);
            var torrentEntity = await CreateTorrentEntity(torrent, description, category, filePath, userId);

            _context.Torrents.Add(torrentEntity);
            await _context.SaveChangesAsync();

            // Grant SakuraCoins to the uploader
            await _userService.AddSakuraCoinsAsync(torrentEntity.UploadedByUserId, _sakuraCoinSettings.UploadTorrentBonus);

            _logger.LogInformation("Torrent {TorrentName} (InfoHash: {InfoHash}) uploaded successfully by user {UserId}.", torrent.DisplayName, infoHash, userId);
            return (true, "Torrent uploaded successfully.", infoHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during torrent upload for file {FileName}.", torrentFile.FileName);
            return (false, "An error occurred during upload.", null);
        }
    }

    public async Task<(bool Success, string Message)> SetFreeAsync(int torrentId, DateTime? freeUntil)
    {
        _logger.LogInformation("SetFree request received for torrentId: {TorrentId}, freeUntil: {FreeUntil}", torrentId, freeUntil);
        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            return (false, "Torrent not found.");
        }

        torrent.IsFree = true;
        torrent.FreeUntil = freeUntil;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Torrent {TorrentName} (Id: {TorrentId}) set to free until {FreeUntil}.", torrent.Name, torrentId, freeUntil?.ToString() ?? "forever");

        return (true, $"Torrent {torrent.Name} set to free until {freeUntil?.ToString() ?? "forever"}.");
    }

    public async Task<(bool Success, string Message)> SetStickyAsync(int torrentId, TorrentStickyStatus status)
    {
        _logger.LogInformation("SetSticky request received for torrentId: {TorrentId}, status: {Status}", torrentId, status);
        if (status == TorrentStickyStatus.None)
        {
            return (false, "Cannot set sticky status to None using this endpoint. Use a dedicated unsticky endpoint if needed.");
        }

        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            return (false, "Torrent not found.");
        }

        torrent.StickyStatus = status;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Torrent {TorrentName} (Id: {TorrentId}) sticky status set to {Status}.", torrent.Name, torrentId, status);

        return (true, $"Torrent {torrent.Name} sticky status set to {status}.");
    }

    public async Task<(bool Success, string Message)> CompleteTorrentInfoAsync(int torrentId, string imdbId, int userId)
    {
        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            return (false, "Torrent not found.");
        }

        if (!string.IsNullOrEmpty(torrent.ImdbId))
        {
            return (false, "IMDb ID already exists for this torrent.");
        }

        torrent.ImdbId = imdbId;

        await _userService.AddSakuraCoinsAsync(userId, _sakuraCoinSettings.CompleteInfoBonus);

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} added IMDb ID {ImdbId} to torrent {TorrentId} and earned {Bonus} SakuraCoins.", userId, imdbId, torrentId, _sakuraCoinSettings.CompleteInfoBonus);

        return (true, "IMDb ID added successfully.");
    }

    public async Task<(bool Success, string Message)> ApplyFreeleechTokenAsync(int torrentId, int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return (false, "User not found.");
        }

        // Check if user has enough SakuraCoins
        if (user.SakuraCoins < _sakuraCoinSettings.FreeleechTokenPrice)
        {
            return (false, "Insufficient SakuraCoins to apply freeleech.");
        }

        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            return (false, "Torrent not found.");
        }

        // Check if the user is seeding this torrent
        var isSeeding = await _context.Peers.AnyAsync(p => p.UserId == userId && p.TorrentId == torrentId && p.IsSeeder);
        if (!isSeeding)
        {
            return (false, "You must be seeding this torrent to apply freeleech.");
        }

        // Apply freeleech status
        torrent.IsFree = true;
        torrent.FreeUntil = DateTime.UtcNow.AddHours(_sakuraCoinSettings.FreeleechTokenDurationHours);

        // Deduct coins directly
        user.SakuraCoins -= _sakuraCoinSettings.FreeleechTokenPrice;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} applied freeleech to torrent {TorrentId} for {Price} SakuraCoins. Free until: {FreeUntil}", userId, torrentId, _sakuraCoinSettings.FreeleechTokenPrice, torrent.FreeUntil);

        return (true, "Freeleech applied successfully.");
    }

    public async Task<FileStreamResult?> DownloadTorrentAsync(int torrentId)
    {
        _logger.LogInformation("Download request received for torrentId: {TorrentId}.", torrentId);
        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            _logger.LogWarning("Download failed: Torrent {TorrentId} not found.", torrentId);
            return null;
        }

        var filePath = torrent.FilePath;
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogError("Download failed: Torrent file not found on server for torrent {TorrentId} at path {FilePath}.", torrentId, filePath);
            return null;
        }

        try
        {
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            _logger.LogInformation("Serving torrent file {FileName} for torrent {TorrentId}.", torrent.Name + ".torrent", torrentId);
            return new FileStreamResult(fileStream, "application/x-bittorrent") { FileDownloadName = torrent.Name + ".torrent" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving torrent file {FileName} for torrent {TorrentId}.", torrent.Name + ".torrent", torrentId);
            return null;
        }
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
    
    private async Task<Entities.Torrent> CreateTorrentEntity(BencodeNET.Torrents.Torrent torrent, string? description, TorrentCategory category, string filePath, int userId)
    {
        _logger.LogDebug("Creating torrent entity for {TorrentName} (InfoHash: {InfoHash}).", torrent.DisplayName, torrent.GetInfoHash());
        var user = await _context.Users.FindAsync(userId);
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
}
