using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Sakura.PT.Data;
using Sakura.PT.DTOs;
using Sakura.PT.Entities;
using Sakura.PT.Enums;
using System.Text.Json;

namespace Sakura.PT.Services;

public class TorrentListingService : ITorrentListingService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<TorrentListingService> _logger;

    public TorrentListingService(ApplicationDbContext context, IDistributedCache cache, ILogger<TorrentListingService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<Torrent>> GetTorrentsAsync(TorrentFilterDto filter)
    {
        // Generate a unique cache key based on filter parameters
        var cacheKey = $"TorrentsList:{filter.PageNumber}:{filter.PageSize}:{filter.Category}:{filter.SearchTerm}:{filter.SortBy}:{filter.SortOrder}";

        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (cachedData != null)
        {
            _logger.LogDebug("Retrieving torrents from cache for key: {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<List<Torrent>>(cachedData) ?? new List<Torrent>();
        }

        _logger.LogInformation("Cache miss for torrents list for key: {CacheKey}. Querying DB.", cacheKey);

        IQueryable<Torrent> query = _context.Torrents.Where(t => !t.IsDeleted);

        // Apply filters
        if (filter.Category.HasValue)
        {
            query = query.Where(t => t.Category == filter.Category.Value);
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            // Use PostgreSQL full-text search
            query = query.Where(t => t.SearchVector.Matches(filter.SearchTerm));
        }

        // Apply sorting
        query = filter.SortBy.ToLower() switch
        {
            "createdat" => filter.SortOrder.ToLower() == "desc" ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
            "name" => filter.SortOrder.ToLower() == "desc" ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
            "size" => filter.SortOrder.ToLower() == "desc" ? query.OrderByDescending(t => t.Size) : query.OrderBy(t => t.Size),
            _ => query.OrderByDescending(t => t.CreatedAt) // Default sort
        };

        // Apply pagination
        var torrents = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        // Cache the result
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // Cache for 5 minutes
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(torrents), cacheOptions);

        return torrents;
    }
}
