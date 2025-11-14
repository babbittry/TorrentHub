using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TorrentHub.Core.Data;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;
using TorrentHub.Mappers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TorrentHub.Services.Interfaces;
using TorrentHub.Services.Background;

namespace TorrentHub.Services;

public class TorrentListingService : ITorrentListingService
{
    private readonly ApplicationDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<TorrentListingService> _logger;
    private readonly IMeiliSearchService _meiliSearchService;

    public TorrentListingService(ApplicationDbContext context, IConnectionMultiplexer redis, ILogger<TorrentListingService> logger, IMeiliSearchService meiliSearchService)
    {
        _context = context;
        _redis = redis;
        _logger = logger;
        _meiliSearchService = meiliSearchService;
    }

    public async Task<PaginatedResult<TorrentDto>> GetTorrentsAsync(TorrentFilterDto filter)
    {
        _logger.LogInformation("Querying torrents with filter: {@Filter}", filter);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            // TODO: MeiliSearchService does not currently return total count, so pagination is incomplete.
            var searchResults = await _meiliSearchService.SearchAsync<Torrent>("torrents", filter.SearchTerm, filter.Page, filter.PageSize);
            var items = searchResults.Select(t => Mapper.ToTorrentDto(t)).ToList();
            return new PaginatedResult<TorrentDto>
            {
                Items = items,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalItems = items.Count, // This is incorrect, but we don't have the total count.
                TotalPages = items.Any() ? 1 : 0 // Also incorrect.
            };
        }

        List<Torrent> torrents;
        long totalItems;
        var sortBy = filter.SortBy.ToLower();

        if (sortBy == "seeders" || sortBy == "leechers")
        {
            var redisDb = _redis.GetDatabase();
            var sortKey = sortBy == "seeders" ? PeerCountUpdateService.SeedersSortKey : PeerCountUpdateService.LeechersSortKey;
            
            totalItems = await redisDb.SortedSetLengthAsync(sortKey);

            long start = (filter.Page - 1) * filter.PageSize;
            long stop = start + filter.PageSize - 1;

            var redisResult = await redisDb.SortedSetRangeByRankAsync(sortKey, start, stop, Order.Descending);
            if (redisResult.Length == 0)
            {
                return new PaginatedResult<TorrentDto> { Items = new List<TorrentDto>(), Page = filter.Page, PageSize = filter.PageSize, TotalItems = 0, TotalPages = 0 };
            }

            var sortedIds = redisResult.Select(v => (long)v).ToArray();
            
            torrents = await _context.Torrents
                .Include(t => t.UploadedByUser)
                .Where(t => sortedIds.Contains(t.Id))
                .ToListAsync();

            var torrentsDict = torrents.ToDictionary(t => (long)t.Id);
            torrents = sortedIds.Where(id => torrentsDict.ContainsKey(id)).Select(id => torrentsDict[id]).ToList();
        }
        else
        {
            IQueryable<Torrent> query = _context.Torrents
                .Include(t => t.UploadedByUser)
                .Where(t => !t.IsDeleted);

            if (filter.Category.HasValue)
            {
                query = query.Where(t => t.Category == filter.Category.Value);
            }

            totalItems = await query.CountAsync();

            query = sortBy switch
            {
                "createdat" => filter.SortOrder.ToLower() == "desc" ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
                "name" => filter.SortOrder.ToLower() == "desc" ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
                "size" => filter.SortOrder.ToLower() == "desc" ? query.OrderByDescending(t => t.Size) : query.OrderBy(t => t.Size),
                _ => query.OrderByDescending(t => t.CreatedAt)
            };

            torrents = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();
        }

        if (!torrents.Any())
        {
            return new PaginatedResult<TorrentDto> { Items = new List<TorrentDto>(), Page = filter.Page, PageSize = filter.PageSize, TotalItems = 0, TotalPages = 0 };
        }

        var torrentDtos = torrents.Select(Mapper.ToTorrentDto).ToList();
        var torrentIds = torrents.Select(t => t.Id).ToArray();

        var redisDbForHashes = _redis.GetDatabase();
        var redisTorrentIds = torrentIds.Select(id => (RedisValue)id.ToString()).ToArray();

        var seedersTask = redisDbForHashes.HashGetAsync(PeerCountUpdateService.SeedersKey, redisTorrentIds);
        var leechersTask = redisDbForHashes.HashGetAsync(PeerCountUpdateService.LeechersKey, redisTorrentIds);

        await Task.WhenAll(seedersTask, leechersTask);

        var seeders = seedersTask.Result;
        var leechers = leechersTask.Result;
        var dtoDict = torrentDtos.ToDictionary(d => d.Id);

        for (int i = 0; i < torrentIds.Length; i++)
        {
            var id = torrentIds[i];
            if (dtoDict.TryGetValue(id, out var dto))
            {
                if (seeders[i].HasValue)
                {
                    dto.Seeders = (int)seeders[i];
                }
                if (leechers[i].HasValue)
                {
                    dto.Leechers = (int)leechers[i];
                }
            }
        }

        return new PaginatedResult<TorrentDto>
        {
            Items = torrentDtos,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalItems = (int)totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)filter.PageSize)
        };
    }
}
