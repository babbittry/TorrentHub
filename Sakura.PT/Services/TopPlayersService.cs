using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Sakura.PT.Data;
using Sakura.PT.DTOs;
using Sakura.PT.Enums;
using Sakura.PT.Mappers;
using System.Text.Json;

namespace Sakura.PT.Services;

public class TopPlayersService : ITopPlayersService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<TopPlayersService> _logger;

    // Cache keys
    private const string TopUploadedKey = "TopUploadedUsers";
    private const string TopDownloadedKey = "TopDownloadedUsers";
    private const string TopSakuraCoinsKey = "TopSakuraCoinsUsers";
    private const string TopSeedingTimeKey = "TopSeedingTimeUsers";
    private const string TopSeedingSizeKey = "TopSeedingSizeUsers";

    // Cache duration (e.g., 1 hour)
    private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    public TopPlayersService(ApplicationDbContext context, IDistributedCache cache, ILogger<TopPlayersService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetTopPlayersAsync(TopPlayerType type)
    {
        string cacheKey = type switch
        {
            TopPlayerType.Uploaded => TopUploadedKey,
            TopPlayerType.Downloaded => TopDownloadedKey,
            TopPlayerType.SakuraCoins => TopSakuraCoinsKey,
            TopPlayerType.SeedingTime => TopSeedingTimeKey,
            TopPlayerType.SeedingSize => TopSeedingSizeKey,
            _ => throw new ArgumentOutOfRangeException(nameof(type), "Invalid TopPlayerType")
        };

        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (cachedData != null)
        {
            _logger.LogDebug("Retrieving top players from cache: {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<List<UserDto>>(cachedData) ?? new List<UserDto>();
        }

        _logger.LogInformation("Cache miss for top players: {CacheKey}. Refreshing from DB.", cacheKey);
        await RefreshTopPlayersCacheAsync(); // Refresh all caches on a miss

        // Try to get from cache again after refresh
        cachedData = await _cache.GetStringAsync(cacheKey);
        if (cachedData != null)
        {
            return JsonSerializer.Deserialize<List<UserDto>>(cachedData) ?? new List<UserDto>();
        }

        return new List<UserDto>(); // Should not happen if refresh is successful
    }

    public async Task RefreshTopPlayersCacheAsync()
    {
        _logger.LogInformation("Refreshing all top players caches.");

        // Top Uploaded
        var topUploaded = await _context.Users
            .OrderByDescending(u => u.UploadedBytes)
            .Take(10) // Get top 10
            .Select(u => Mapper.ToUserDto(u))
            .ToListAsync();
        await _cache.SetStringAsync(TopUploadedKey, JsonSerializer.Serialize(topUploaded), _cacheOptions);
        _logger.LogInformation("Refreshed Top Uploaded Users cache.");

        // Top Downloaded
        var topDownloaded = await _context.Users
            .OrderByDescending(u => u.DownloadedBytes)
            .Take(10)
            .Select(u => Mapper.ToUserDto(u))
            .ToListAsync();
        await _cache.SetStringAsync(TopDownloadedKey, JsonSerializer.Serialize(topDownloaded), _cacheOptions);
        _logger.LogInformation("Refreshed Top Downloaded Users cache.");

        // Top SakuraCoins
        var topSakuraCoins = await _context.Users
            .OrderByDescending(u => u.SakuraCoins)
            .Take(10)
            .Select(u => Mapper.ToUserDto(u))
            .ToListAsync();
        await _cache.SetStringAsync(TopSakuraCoinsKey, JsonSerializer.Serialize(topSakuraCoins), _cacheOptions);
        _logger.LogInformation("Refreshed Top SakuraCoins Users cache.");

        // Top Seeding Time
        var topSeedingTime = await _context.Users
            .OrderByDescending(u => u.TotalSeedingTimeMinutes)
            .Take(10)
            .Select(u => Mapper.ToUserDto(u))
            .ToListAsync();
        await _cache.SetStringAsync(TopSeedingTimeKey, JsonSerializer.Serialize(topSeedingTime), _cacheOptions);
        _logger.LogInformation("Refreshed Top Seeding Time Users cache.");

        // Top Seeding Size
        // Calculate total seeding size for each user
        var topSeedingSize = await _context.Peers
            .Where(p => p.IsSeeder) // Only consider seeders
            .Include(p => p.Torrent) // Include torrent to get its size
            .GroupBy(p => p.UserId) // Group by user
            .Select(g => new
            {
                UserId = g.Key,
                TotalSeedingSize = g.Sum(p => p.Torrent!.Size) // Sum up sizes of unique torrents seeded by the user
            })
            .OrderByDescending(x => x.TotalSeedingSize)
            .Take(10)
            .Join(_context.Users, // Join with Users table to get user details
                  peerGroup => peerGroup.UserId,
                  user => user.Id,
                  (peerGroup, user) => Mapper.ToUserDto(user) // Map to UserDto
            )
            .ToListAsync();
        await _cache.SetStringAsync(TopSeedingSizeKey, JsonSerializer.Serialize(topSeedingSize), _cacheOptions);
        _logger.LogInformation("Refreshed Top Seeding Size Users cache.");
    }
}
