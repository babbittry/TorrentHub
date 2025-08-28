using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TorrentHub.Core.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace TorrentHub.Services;

public class PeerCountUpdateService : BackgroundService
{
    private readonly ILogger<PeerCountUpdateService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _period = TimeSpan.FromSeconds(60); // Update every 60 seconds

    public const string SeedersKey = "torrents:seeders";
    public const string LeechersKey = "torrents:leechers";
    public const string SeedersSortKey = "sort:seeders";
    public const string LeechersSortKey = "sort:leechers";

    public PeerCountUpdateService(ILogger<PeerCountUpdateService> logger, IServiceProvider serviceProvider, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _redis = redis;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Peer Count Update Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdatePeerCounts(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating peer counts.");
            }

            await Task.Delay(_period, stoppingToken);
        }

        _logger.LogInformation("Peer Count Update Service is stopping.");
    }

    private async Task UpdatePeerCounts(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting peer count update cycle.");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var redisDb = _redis.GetDatabase();

        // Efficiently get all peer counts
        var peerCounts = await dbContext.Peers
            .AsNoTracking()
            .GroupBy(p => p.TorrentId)
            .Select(g => new
            {
                TorrentId = g.Key,
                Seeders = g.Count(p => p.IsSeeder),
                Leechers = g.Count(p => !p.IsSeeder)
            })
            .ToListAsync(stoppingToken);

        _logger.LogDebug("Fetched peer counts for {Count} torrents.", peerCounts.Count);

        var seederHashEntries = new List<HashEntry>();
        var leecherHashEntries = new List<HashEntry>();
        var seederSortSetEntries = new List<SortedSetEntry>();
        var leecherSortSetEntries = new List<SortedSetEntry>();

        foreach (var count in peerCounts)
        {
            seederHashEntries.Add(new HashEntry(count.TorrentId.ToString(), count.Seeders));
            leecherHashEntries.Add(new HashEntry(count.TorrentId.ToString(), count.Leechers));
            seederSortSetEntries.Add(new SortedSetEntry(count.TorrentId.ToString(), count.Seeders));
            leecherSortSetEntries.Add(new SortedSetEntry(count.TorrentId.ToString(), count.Leechers));
        }

        // Use a transaction to update Redis
        var tran = redisDb.CreateTransaction();
        
        // Clear old data
        await tran.KeyDeleteAsync(SeedersKey);
        await tran.KeyDeleteAsync(LeechersKey);
        await tran.KeyDeleteAsync(SeedersSortKey);
        await tran.KeyDeleteAsync(LeechersSortKey);
        
        // Set new hash values
        if (seederHashEntries.Any())
        {
            await tran.HashSetAsync(SeedersKey, seederHashEntries.ToArray());
        }
        if (leecherHashEntries.Any())
        {
            await tran.HashSetAsync(LeechersKey, leecherHashEntries.ToArray());
        }

        // Set new sorted set values
        if (seederSortSetEntries.Any())
        {
            await tran.SortedSetAddAsync(SeedersSortKey, seederSortSetEntries.ToArray());
        }
        if (leecherSortSetEntries.Any())
        {
            await tran.SortedSetAddAsync(LeechersSortKey, leecherSortSetEntries.ToArray());
        }
        
        var executed = await tran.ExecuteAsync();

        if (executed)
        {
            _logger.LogInformation("Successfully updated peer counts for {Count} torrents in Redis (Hashes and Sorted Sets).", peerCounts.Count);
        }
        else
        {
            _logger.LogError("Failed to execute Redis transaction for peer count update.");
        }
    }
}
