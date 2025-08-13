using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using TorrentHub.Enums;
using System.Text.Json;
using TorrentHub.Data;
using TorrentHub.DTOs;
using TorrentHub.Entities;
using TorrentHub.Mappers;

namespace TorrentHub.Services;

public class TorrentListingService : ITorrentListingService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<TorrentListingService> _logger;
    private readonly IElasticsearchService _elasticsearchService;

    public TorrentListingService(ApplicationDbContext context, IDistributedCache cache, ILogger<TorrentListingService> logger, IElasticsearchService elasticsearchService)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
        _elasticsearchService = elasticsearchService;
    }

    public async Task<List<TorrentDto>> GetTorrentsAsync(TorrentFilterDto filter)
    {
        // Generate a unique cache key based on filter parameters
        var cacheKey = $"TorrentsList:{filter.PageNumber}:{filter.PageSize}:{filter.Category}:{filter.SearchTerm}:{filter.SortBy}:{filter.SortOrder}";

        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (cachedData != null)
        {
            _logger.LogDebug("Retrieving torrents from cache for key: {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<List<TorrentDto>>(cachedData) ?? new List<TorrentDto>();
        }

        _logger.LogInformation("Cache miss for torrents list for key: {CacheKey}. Querying DB.", cacheKey);

        IQueryable<Torrent> query = _context.Torrents
            .Include(t => t.UploadedByUser) // Include the uploader's information
            .Where(t => !t.IsDeleted);

        // Apply filters
        if (filter.Category.HasValue)
        {
            query = query.Where(t => t.Category == filter.Category.Value);
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            return (await _elasticsearchService.SearchTorrentsAsync(filter.SearchTerm, filter.PageNumber, filter.PageSize)).Select(t => Mapper.ToTorrentDto(t)).ToList();
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

        // Get peer counts efficiently
        var torrentIds = torrents.Select(t => t.Id).ToList();
        var peerCounts = await _context.Peers
            .Where(p => torrentIds.Contains(p.TorrentId))
            .GroupBy(p => p.TorrentId)
            .Select(g => new
            {
                TorrentId = g.Key,
                Seeders = g.Count(p => p.IsSeeder),
                Leechers = g.Count(p => !p.IsSeeder)
            })
            .ToDictionaryAsync(x => x.TorrentId, x => x);

        var torrentDtos = torrents.Select(t =>
        {
            var dto = Mapper.ToTorrentDto(t);
            if (peerCounts.TryGetValue(t.Id, out var counts))
            {
                dto.Seeders = counts.Seeders;
                dto.Leechers = counts.Leechers;
            }
            else
            {
                dto.Seeders = 0;
                dto.Leechers = 0;
            }
            return dto;
        }).ToList();

        // Cache the result
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // Cache for 5 minutes
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(torrentDtos), cacheOptions);

        return torrentDtos;
    }
}
