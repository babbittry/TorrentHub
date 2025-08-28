using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Enums;

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
        var totalBannedUsers = await _context.Users.LongCountAsync(u => u.BanStatus != BanStatus.None);
        var totalUsers = await _context.Users.LongCountAsync();

        var staffRoles = new[] { UserRole.Moderator, UserRole.Administrator };
        var userRoleCountsFromDb = await _context.Users
            .Where(u => u.BanStatus == BanStatus.None && !staffRoles.Contains(u.Role))
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(x => x.Role, x => x.Count);

        var userRoleCounts = Enum.GetValues(typeof(UserRole))
            .Cast<UserRole>()
            .Where(r => !staffRoles.Contains(r))
            .ToDictionary(
                r => r.ToString(),
                r => userRoleCountsFromDb.TryGetValue(r, out var count) ? count : 0L
            );

        var totalTorrents = await _context.Torrents.LongCountAsync();
        var deadTorrents = await _context.Torrents.LongCountAsync(t => t.Seeders == 0);
        var totalTorrentsSize = (ulong)await _context.Torrents.SumAsync(t => t.Size);
        var totalPeers = await _context.Peers.LongCountAsync();

        var uploadStats = await _context.Users
            .GroupBy(u => 1)
            .OrderBy(g => g.Key) // Add OrderBy to remove EF Core warning
            .Select(g => new
            {
                TotalUploaded = (ulong)g.Sum(u => (decimal)u.UploadedBytes),
                TotalDownloaded = (ulong)g.Sum(u => (decimal)u.DownloadedBytes),
                NominalUploaded = (ulong)g.Sum(u => (decimal)u.NominalUploadedBytes),
                NominalDownloaded = (ulong)g.Sum(u => (decimal)u.NominalDownloadedBytes)
            })
            .FirstOrDefaultAsync();

        var today = DateTimeOffset.UtcNow.Date;
        var usersRegisteredToday = await _context.Users.LongCountAsync(u => u.CreatedAt.Date == today);
        var torrentsAddedToday = await _context.Torrents.LongCountAsync(t => t.CreatedAt.Date == today);

        var requestStats = await _context.Requests
            .GroupBy(r => 1)
            .OrderBy(g => g.Key) // Add OrderBy to remove EF Core warning
            .Select(g => new
            {
                TotalRequests = g.LongCount(),
                FilledRequests = g.LongCount(r => r.Status == RequestStatus.Filled)
            })
            .FirstOrDefaultAsync();

        var totalForumTopics = await _context.ForumTopics.LongCountAsync();
        var totalForumPosts = await _context.ForumPosts.LongCountAsync();

        var peerStats = await _context.Torrents
            .GroupBy(t => 1)
            .OrderBy(g => g.Key) // Add OrderBy to remove EF Core warning
            .Select(g => new
            {
                TotalSeeders = g.Sum(t => (long)t.Seeders),
                TotalLeechers = g.Sum(t => (long)t.Leechers)
            })
            .FirstOrDefaultAsync();

        return new SiteStatsDto
        {
            TotalUsers = totalUsers,
            TotalBannedUsers = totalBannedUsers,
            UserRoleCounts = userRoleCounts,
            TotalTorrents = totalTorrents,
            DeadTorrents = deadTorrents,
            TotalTorrentsSize = totalTorrentsSize,
            TotalPeers = totalPeers,
            TotalUploaded = uploadStats?.TotalUploaded ?? 0UL,
            TotalDownloaded = uploadStats?.TotalDownloaded ?? 0UL,
            NominalUploaded = uploadStats?.NominalUploaded ?? 0UL,
            NominalDownloaded = uploadStats?.NominalDownloaded ?? 0UL,
            UsersRegisteredToday = usersRegisteredToday,
            TorrentsAddedToday = torrentsAddedToday,
            TotalRequests = requestStats?.TotalRequests ?? 0,
            FilledRequests = requestStats?.FilledRequests ?? 0,
            TotalForumTopics = totalForumTopics,
            TotalForumPosts = totalForumPosts,
            TotalSeeders = peerStats?.TotalSeeders ?? 0,
            TotalLeechers = peerStats?.TotalLeechers ?? 0
        };
    }
}

