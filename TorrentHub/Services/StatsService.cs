using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using TorrentHub.Data;
using TorrentHub.DTOs;
using TorrentHub.Enums;

namespace TorrentHub.Services;

public class StatsService : IStatsService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<StatsService> _logger;
    private const string SiteStatsCacheKey = "SiteStats";

    public StatsService(ApplicationDbContext context, IDistributedCache cache, ILogger<StatsService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<SiteStatsDto> GetSiteStatsAsync()
    {
        string? cachedStatsJson = null;
        try
        {
            cachedStatsJson = await _cache.GetStringAsync(SiteStatsCacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving site stats from cache.");
        }

        if (!string.IsNullOrEmpty(cachedStatsJson))
        {
            _logger.LogInformation("Site stats retrieved from cache.");
            return JsonSerializer.Deserialize<SiteStatsDto>(cachedStatsJson) ?? new SiteStatsDto();
        }

        _logger.LogInformation("Cache miss for site stats. Recalculating and caching.");
        return await RecalculateAndCacheSiteStatsAsync();
    }

    public async Task<SiteStatsDto> RecalculateAndCacheSiteStatsAsync()
    {
        _logger.LogInformation("Recalculating site stats from database.");
        var stats = await FetchStatsFromDbAsync();

        try
        {
            var statsJson = JsonSerializer.Serialize(stats);
            var cacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(35) // A bit longer than the refresh interval
            };
            await _cache.SetStringAsync(SiteStatsCacheKey, statsJson, cacheEntryOptions);
            _logger.LogInformation("Site stats cached successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching site stats.");
        }

        return stats;
    }

    private async Task<SiteStatsDto> FetchStatsFromDbAsync()
    {
        var totalUsers = await _context.Users.LongCountAsync();

        var normalUsers = await _context.Users
            .LongCountAsync(u => u.Role != UserRole.Moderator && u.Role != UserRole.Administrator);

        var totalTorrents = await _context.Torrents.LongCountAsync();

        var deadTorrents = await _context.Torrents.LongCountAsync(t => t.Seeders == 0);

        var totalTorrentsSize = (ulong)await _context.Torrents.SumAsync(t => t.Size);

        var totalPeers = await _context.Peers.LongCountAsync();

        var uploadStats = await _context.Users
            .GroupBy(u => 1)
            .Select(g => new
            {
                TotalUploaded = (ulong)g.Sum(u => (decimal)u.UploadedBytes),
                TotalDownloaded = (ulong)g.Sum(u => (decimal)u.DownloadedBytes),
                DisplayTotalUploaded = (ulong)g.Sum(u => (decimal)u.DisplayUploadedBytes),
                DisplayTotalDownloaded = (ulong)g.Sum(u => (decimal)u.DisplayDownloadedBytes)
            })
            .FirstOrDefaultAsync();

        return new SiteStatsDto
        {
            TotalUsers = totalUsers,
            UserRoleCounts = userRoleCounts,
            TotalTorrents = totalTorrents,
            DeadTorrents = deadTorrents,
            TotalTorrentsSize = totalTorrentsSize,
            TotalPeers = totalPeers,
            TotalUploaded = uploadStats?.TotalUploaded ?? 0UL,
            TotalDownloaded = uploadStats?.TotalDownloaded ?? 0UL,
            DisplayTotalUploaded = uploadStats?.DisplayTotalUploaded ?? 0UL,
            DisplayTotalDownloaded = uploadStats?.DisplayTotalDownloaded ?? 0UL
        };
    }
}
