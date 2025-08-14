
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TorrentHub.Data;
using TorrentHub.DTOs;
using TorrentHub.Entities;
using TorrentHub.Mappers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TorrentHub.Services;

public class TorrentListingService : ITorrentListingService
{
    private readonly ApplicationDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<TorrentListingService> _logger;
    private readonly IElasticsearchService _elasticsearchService;

    public TorrentListingService(ApplicationDbContext context, IConnectionMultiplexer redis, ILogger<TorrentListingService> logger, IElasticsearchService elasticsearchService)
    {
        _context = context;
        _redis = redis;
        _logger = logger;
        _elasticsearchService = elasticsearchService;
    }

    public async Task<List<TorrentDto>> GetTorrentsAsync(TorrentFilterDto filter)
    {
        _logger.LogInformation("Querying torrents with filter: {@Filter}", filter);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            // TODO: When using Elasticsearch, we need to enrich the results with peer counts from Redis as well.
            return (await _elasticsearchService.SearchTorrentsAsync(filter.SearchTerm, filter.PageNumber, filter.PageSize)).Select(t => Mapper.ToTorrentDto(t)).ToList();
        }

        List<Torrent> torrents;
        long[]? sortedIds = null;
        var sortBy = filter.SortBy.ToLower();

        if (sortBy == "seeders" || sortBy == "leechers")
        {
            var redisDb = _redis.GetDatabase();
            var sortKey = sortBy == "seeders" ? PeerCountUpdateService.SeedersSortKey : PeerCountUpdateService.LeechersSortKey;
            
            long start = (filter.PageNumber - 1) * filter.PageSize;
            long stop = start + filter.PageSize - 1;

            var redisResult = await redisDb.SortedSetRangeByRankAsync(sortKey, start, stop, Order.Descending);
            if (redisResult.Length == 0)
            {
                return new List<TorrentDto>();
            }

            sortedIds = redisResult.Select(v => (long)v).ToArray();
            
            torrents = await _context.Torrents
                .Include(t => t.UploadedByUser)
                .Where(t => sortedIds.Contains(t.Id))
                .ToListAsync();

            // Re-order the results to match the order from Redis
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

            query = sortBy switch
            {
                "createdat" => filter.SortOrder.ToLower() == "desc" ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
                "name" => filter.SortOrder.ToLower() == "desc" ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
                "size" => filter.SortOrder.ToLower() == "desc" ? query.OrderByDescending(t => t.Size) : query.OrderBy(t => t.Size),
                _ => query.OrderByDescending(t => t.CreatedAt)
            };

            torrents = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();
        }

        if (!torrents.Any())
        {
            return new List<TorrentDto>();
        }

        var torrentDtos = torrents.Select(Mapper.ToTorrentDto).ToList();
        var torrentIds = torrents.Select(t => t.Id).ToArray();

        // Get peer counts from Redis Hashes
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

        return torrentDtos;
    }
}
