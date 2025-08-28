
using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Mappers;
using TorrentHub.Core.Enums;
using TorrentHub.Core.Services;


namespace TorrentHub.Services;

public class TorrentService : ITorrentService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly ITMDbService _tmdbService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TorrentService> _logger;
    private readonly IMeiliSearchService _meiliSearchService;
    private readonly BencodeParser _bencodeParser = new();

    public TorrentService(
        ApplicationDbContext context, 
        IUserService userService, 
        ILogger<TorrentService> logger, 
        IMeiliSearchService meiliSearchService, 
        ITMDbService tmdbService,
        ISettingsService settingsService)
    {
        _context = context;
        _userService = userService;
        _logger = logger;
        _meiliSearchService = meiliSearchService;
        _tmdbService = tmdbService;
        _settingsService = settingsService;
    }

    public async Task<(bool Success, string Message, string? InfoHash)> UploadTorrentAsync(IFormFile torrentFile, UploadTorrentRequestDto request, int userId)
    {
        _logger.LogInformation("Upload request received for file: {FileName}, category: {Category}, user: {UserId}", torrentFile.FileName, request.Category, userId);

        if (torrentFile.Length == 0)
        {
            return (false, "error.torrent.empty", null);
        }

        var settings = await _settingsService.GetSiteSettingsAsync();
        if (torrentFile.Length > settings.MaxTorrentSize)
        {
            return (false, "error.torrent.tooLarge", null);
        }

        var torrent = await ParseTorrentFile(torrentFile);
        if (torrent == null)
        {
            return (false, "error.torrent.invalidFile", null);
        }

        var infoHashBytes = torrent.GetInfoHashBytes();
        var infoHash = BitConverter.ToString(infoHashBytes).Replace("-", "").ToLowerInvariant();
        if (await _context.Torrents.AnyAsync(t => t.InfoHash == infoHashBytes))
        {
            return (false, "error.torrent.alreadyExists", null);
        }

        try
        {
            var filePath = await SaveTorrentFile(torrentFile, infoHash, settings.TorrentStoragePath);
            var torrentEntity = await CreateTorrentEntity(torrent, request, filePath, userId, infoHashBytes);

            _context.Torrents.Add(torrentEntity);
            await _context.SaveChangesAsync();

            // Grant Coins to the uploader
            await _userService.AddCoinsAsync(torrentEntity.UploadedByUserId, new UpdateCoinsRequestDto { Amount = settings.UploadTorrentBonus });

            var torrentSearchDto = Mapper.ToTorrentSearchDto(torrentEntity);
            await _meiliSearchService.IndexTorrentAsync(torrentSearchDto);

            _logger.LogInformation("Torrent {TorrentName} (InfoHash: {InfoHash}) uploaded successfully by user {UserId}.", torrent.DisplayName, infoHash, userId);
            return (true, "torrent.upload.success", infoHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during torrent upload for file {FileName}.", torrentFile.FileName);
            return (false, "error.torrent.uploadFailed", null);
        }
    }

    private async Task<TorrentHub.Core.Entities.Torrent> CreateTorrentEntity(BencodeNET.Torrents.Torrent torrent, UploadTorrentRequestDto request, string filePath, int userId, byte[] infoHashBytes)
    {
        _logger.LogDebug("Creating torrent entity for {TorrentName} (InfoHash: {InfoHash}).", torrent.DisplayName, torrent.GetInfoHash());
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogError("User {UserId} not found in database during torrent entity creation.", userId);
            throw new Exception("User not found");
        }

        var torrentEntity = new TorrentHub.Core.Entities.Torrent
        {
            Name = torrent.DisplayName,
            InfoHash = infoHashBytes,
            Description = request.Description,
            Size = torrent.TotalSize,
            UploadedByUserId = user.Id,
            UploadedByUser = user,
            Category = request.Category,
            CreatedAt = DateTimeOffset.UtcNow,
            IsFree = false,
            FreeUntil = null,
            StickyStatus = TorrentStickyStatus.None,
            FilePath = filePath,
            ImdbId = request.ImdbId,
            Genres = new List<string>()
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
                torrentEntity.Genres = movieData.Genres?.Select(g => g.Name).ToList() ?? new List<string>();
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

    public async Task<TorrentHub.Core.Entities.Torrent?> GetTorrentByIdAsync(int torrentId)
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
            return (false, "error.torrent.notFound");
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return (false, "error.user.notFound");
        }

        if (torrent.UploadedByUserId != userId && user.Role != UserRole.Administrator)
        {
            return (false, "error.unauthorized");
        }

        _context.Torrents.Remove(torrent);
        await _context.SaveChangesAsync();

        await _meiliSearchService.DeleteTorrentAsync(torrentId);

        _logger.LogInformation("Torrent {TorrentId} deleted by user {UserId}.", torrentId, userId);
        return (true, "torrent.delete.success");
    }

    public async Task<(bool Success, string Message)> SetFreeAsync(int torrentId, DateTime freeUntil)
    {
        _logger.LogInformation("SetFree request received for torrentId: {TorrentId}, freeUntil: {FreeUntil}", torrentId, freeUntil);
        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            return (false, "error.torrent.notFound");
        }

        torrent.IsFree = true;
        torrent.FreeUntil = freeUntil;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Torrent {TorrentName} (Id: {TorrentId}) set to free until {FreeUntil}.", torrent.Name, torrentId, freeUntil.ToString());

        return (true, "torrent.setFree.success");
    }

    public async Task<(bool Success, string Message)> SetStickyAsync(int torrentId, SetStickyRequestDto request)
    {
        _logger.LogInformation("SetSticky request received for torrentId: {TorrentId}, status: {Status}", torrentId, request.Status);
        if (request.Status == TorrentStickyStatus.None)
        { 
            return (false, "error.torrent.cannotSetStickyNone");
        }

        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            return (false, "error.torrent.notFound");
        }

        torrent.StickyStatus = request.Status;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Torrent {TorrentName} (Id: {TorrentId}) sticky status set to {Status}.", torrent.Name, torrentId, request.Status);

        return (true, "torrent.setSticky.success");
    }

    public async Task<(bool Success, string Message)> CompleteTorrentInfoAsync(int torrentId, CompleteInfoRequestDto request, int userId)
    {
        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            return (false, "error.torrent.notFound");
        }

        if (!string.IsNullOrEmpty(torrent.ImdbId))
        {
            return (false, "error.torrent.imdbIdExists");
        }

        torrent.ImdbId = request.ImdbId;

        // Note: CompleteInfoBonus is not in settings yet.
        // var settings = await _settingsService.GetSiteSettingsAsync();
        // await _userService.AddCoinsAsync(userId, new UpdateCoinsRequestDto { Amount = settings.CompleteInfoBonus });

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} added IMDb ID {ImdbId} to torrent {TorrentId}.", userId, request.ImdbId, torrentId);

        return (true, "torrent.completeInfo.success");
    }

    public async Task<(bool Success, string Message)> ApplyFreeleechAsync(int torrentId, int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return (false, "error.user.notFound");
        }

        var settings = await _settingsService.GetSiteSettingsAsync();
        // Note: FreeleechPrice and FreeleechDurationHours are not in settings yet.
        // if (user.Coins < settings.FreeleechPrice)
        // {
        //     return (false, "error.request.insufficientCoins");
        // }

        var torrent = await _context.Torrents.FindAsync(torrentId);
        if (torrent == null)
        {
            return (false, "error.torrent.notFound");
        }

        var isSeeding = await _context.Peers.AnyAsync(p => p.UserId == userId && p.TorrentId == torrentId && p.IsSeeder);
        if (!isSeeding)
        {
            return (false, "error.torrent.notSeeding");
        }

        torrent.IsFree = true;
        // torrent.FreeUntil = DateTimeOffset.UtcNow.AddHours(settings.FreeleechDurationHours);

        // user.Coins -= settings.FreeleechPrice;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} applied freeleech to torrent {TorrentId}. Free until: {FreeUntil}", userId, torrentId, torrent.FreeUntil);

        return (true, "torrent.applyFreeleech.success");
    }

    public async Task<FileStreamResult?> DownloadTorrentAsync(int torrentId, int userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null || user.BanStatus.HasFlag(BanStatus.DownloadBan) || user.BanStatus.HasFlag(BanStatus.LoginBan))
        {
            _logger.LogWarning("Download forbidden for user {UserId} for torrent {TorrentId}. BanStatus: {BanStatus}", userId, torrentId, user?.BanStatus);
            return null;
        }

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

    private async Task<string> SaveTorrentFile(IFormFile file, string infoHash, string torrentStoragePath)
    {
        if (!Directory.Exists(torrentStoragePath))
        {
            _logger.LogInformation("Creating torrents directory: {Directory}", torrentStoragePath);
            Directory.CreateDirectory(torrentStoragePath);
        }

        var filePath = Path.Combine(torrentStoragePath, $"{infoHash}.torrent");
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
            throw;
        }
    }
}

