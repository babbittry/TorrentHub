using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NpgsqlTypes;
using TorrentHub.Data;
using TorrentHub.DTOs;
using TorrentHub.Entities;
using TorrentHub.Enums;
using Torrent = TorrentHub.Entities.Torrent;

namespace TorrentHub.Services;

public class TorrentService : ITorrentService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly ITMDbService _tmdbService;
    private readonly CoinSettings _coinSettings;
    private readonly ILogger<TorrentService> _logger;
    private readonly TorrentSettings _torrentSettings;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly BencodeParser _bencodeParser = new();

    public TorrentService(ApplicationDbContext context, IUserService userService, IOptions<CoinSettings> coinSettings, ILogger<TorrentService> logger, IOptions<TorrentSettings> torrentSettings, IElasticsearchService elasticsearchService, ITMDbService tmdbService)
    {
        _context = context;
        _userService = userService;
        _coinSettings = coinSettings.Value;
        _logger = logger;
        _torrentSettings = torrentSettings.Value;
        _elasticsearchService = elasticsearchService;
        _tmdbService = tmdbService;
    }

    public async Task<(bool Success, string Message, string? InfoHash)> UploadTorrentAsync(IFormFile torrentFile, UploadTorrentRequestDto request, int userId)
    {
        _logger.LogInformation("Upload request received for file: {FileName}, category: {Category}, user: {UserId}", torrentFile.FileName, request.Category, userId);

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
            var torrentEntity = await CreateTorrentEntity(torrent, request, filePath, userId);

            _context.Torrents.Add(torrentEntity);
            await _context.SaveChangesAsync();

            // Grant Coins to the uploader
            await _userService.AddCoinsAsync(torrentEntity.UploadedByUserId, new UpdateCoinsRequestDto { Amount = _coinSettings.UploadTorrentBonus });

            // Index torrent in Elasticsearch
            await _elasticsearchService.IndexTorrentAsync(torrentEntity);

            _logger.LogInformation("Torrent {TorrentName} (InfoHash: {InfoHash}) uploaded successfully by user {UserId}.", torrent.DisplayName, infoHash, userId);
            return (true, "Torrent uploaded successfully.", infoHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during torrent upload for file {FileName}.", torrentFile.FileName);
            return (false, "An error occurred during upload.", null);
        }
    }

    private async Task<Torrent> CreateTorrentEntity(BencodeNET.Torrents.Torrent torrent, UploadTorrentRequestDto request, string filePath, int userId)
    {
        _logger.LogDebug("Creating torrent entity for {TorrentName} (InfoHash: {InfoHash}).", torrent.DisplayName, torrent.GetInfoHash());
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogError("User {UserId} not found in database during torrent entity creation.", userId);
            throw new Exception("User not found");
        }

        var torrentEntity = new Torrent
        {
            Name = torrent.DisplayName,
            InfoHash = torrent.GetInfoHash(),
            Description = request.Description,
            Size = torrent.TotalSize,
            UploadedByUserId = user.Id,
            UploadedByUser = user,
            Category = request.Category,
            CreatedAt = DateTime.UtcNow,
            IsFree = false,
            FreeUntil = null,
            StickyStatus = TorrentStickyStatus.None,
            FilePath = filePath,
            ImdbId = request.ImdbId
        };

        if (!string.IsNullOrWhiteSpace(request.ImdbId))
        {
            _logger.LogInformation("Fetching movie data from TMDb for IMDb ID: {ImdbId}", request.ImdbId);
            var movieData = await _tmdbService.GetMovieByImdbIdAsync(request.ImdbId);
            if (movieData != null)
            {
                _logger.LogInformation("Successfully fetched data for movie: {MovieTitle}", movieData.Title);
                torrentEntity.Name = movieData.Title ?? torrentEntity.Name;
                torrentEntity.Description = movieData.Overview;
                torrentEntity.TMDbId = movieData.Id;
                torrentEntity.OriginalTitle = movieData.OriginalTitle;
                torrentEntity.Tagline = movieData.Tagline;
                if (int.TryParse(movieData.ReleaseDate?.Split('-').FirstOrDefault(), out var year))
                {
                    torrentEntity.Year = year;
                }
                torrentEntity.PosterPath = movieData.PosterPath;
                torrentEntity.BackdropPath = movieData.BackdropPath;
                torrentEntity.Runtime = movieData.Runtime;
                torrentEntity.Rating = movieData.VoteAverage;
                torrentEntity.Genres = movieData.Genres != null ? string.Join(", ", movieData.Genres.Select(g => g.Name)) : null;
                torrentEntity.Directors = movieData.Credits?.Crew != null ? string.Join(", ", movieData.Credits.Crew.Where(c => c.Job == "Director").Select(c => c.Name)) : null;
                torrentEntity.Cast = movieData.Credits?.Cast != null ? string.Join(", ", movieData.Credits.Cast.OrderBy(c => c.Order).Take(5).Select(c => c.Name)) : null;
            }
            else
            {
                _logger.LogWarning("Could not fetch movie data for IMDb ID: {ImdbId}", request.ImdbId);
            }
        }

        return torrentEntity;
    }

    // ... other methods from the original file ...
    public async Task<Torrent?> GetTorrentByIdAsync(int torrentId)
    {
        return await _context.Torrents
            .Include(t => t.UploadedByUser)
            .FirstOrDefaultAsync(t => t.Id == torrentId);
    }

    public async Task<(bool Success, string Message)> DeleteTorrentAsync(int torrentId, int userId)
    {
        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            return (false, "Torrent not found.");
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found.");
        }

        // Only the uploader or an admin can delete the torrent
        if (torrent.UploadedByUserId != userId && user.Role != UserRole.Administrator)
        {
            return (false, "Unauthorized to delete this torrent.");
        }

        _context.Torrents.Remove(torrent);
        await _context.SaveChangesAsync();

        // Delete from Elasticsearch
        await _elasticsearchService.DeleteTorrentAsync(torrentId);

        _logger.LogInformation("Torrent {TorrentId} deleted by user {UserId}.", torrentId, userId);
        return (true, "Torrent deleted successfully.");
    }

    public async Task<(bool Success, string Message)> SetFreeAsync(int torrentId, DateTime freeUntil)
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
        _logger.LogInformation("Torrent {TorrentName} (Id: {TorrentId}) set to free until {FreeUntil}.", torrent.Name, torrentId, freeUntil.ToString());

        return (true, $"Torrent {torrent.Name} set to free until {freeUntil.ToString()}.");
    }

    public async Task<(bool Success, string Message)> SetStickyAsync(int torrentId, SetStickyRequestDto request)
    {
        _logger.LogInformation("SetSticky request received for torrentId: {TorrentId}, status: {Status}", torrentId, request.Status);
        if (request.Status == TorrentStickyStatus.None)
        { 
            return (false, "Cannot set sticky status to None using this endpoint. Use a dedicated unsticky endpoint if needed.");
        }

        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            return (false, "Torrent not found.");
        }

        torrent.StickyStatus = request.Status;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Torrent {TorrentName} (Id: {TorrentId}) sticky status set to {Status}.", torrent.Name, torrentId, request.Status);

        return (true, $"Torrent {torrent.Name} sticky status set to {request.Status}.");
    }

    public async Task<(bool Success, string Message)> CompleteTorrentInfoAsync(int torrentId, CompleteInfoRequestDto request, int userId)
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

        torrent.ImdbId = request.ImdbId;

        await _userService.AddCoinsAsync(userId, new UpdateCoinsRequestDto { Amount = _coinSettings.CompleteInfoBonus });

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} added IMDb ID {ImdbId} to torrent {TorrentId} and earned {Bonus} Coins.", userId, request.ImdbId, torrentId, _coinSettings.CompleteInfoBonus);

        return (true, "IMDb ID added successfully.");
    }

    public async Task<(bool Success, string Message)> ApplyFreeleechAsync(int torrentId, int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return (false, "User not found.");
        }

        // Check if user has enough Coins
        if (user.Coins < _coinSettings.FreeleechPrice)
        {
            return (false, "Insufficient Coins to apply freeleech.");
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
        torrent.FreeUntil = DateTime.UtcNow.AddHours(_coinSettings.FreeleechDurationHours);

        // Deduct coins directly
        user.Coins -= _coinSettings.FreeleechPrice;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} applied freeleech to torrent {TorrentId} for {Price} Coins. Free until: {FreeUntil}", userId, torrentId, _coinSettings.FreeleechPrice, torrent.FreeUntil);

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
        var torrentsDir = _torrentSettings.TorrentStoragePath;
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
}