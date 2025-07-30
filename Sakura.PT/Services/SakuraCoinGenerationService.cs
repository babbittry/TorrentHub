using Microsoft.Extensions.Options;
using Sakura.PT.Data;
using Microsoft.EntityFrameworkCore;

namespace Sakura.PT.Services;

public class SakuraCoinGenerationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SakuraCoinSettings _settings;
    private readonly ILogger<SakuraCoinGenerationService> _logger;

    public SakuraCoinGenerationService(
        IServiceProvider serviceProvider,
        IOptions<SakuraCoinSettings> settings,
        ILogger<SakuraCoinGenerationService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SakuraCoin Generation Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting SakuraCoin generation cycle.");

                await GenerateCoinsAsync();

                _logger.LogInformation("SakuraCoin generation cycle finished. Waiting for the next cycle.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the SakuraCoin generation cycle.");
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.GenerationIntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("SakuraCoin Generation Service is stopping.");
    }

    private async Task GenerateCoinsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var activePeers = await dbContext.Peers
            .Where(p => p.IsSeeder)
            .Include(p => p.Torrent)
            .Include(p => p.User)
            .ToListAsync();

        var userCoinGains = new Dictionary<int, double>();

        foreach (var peer in activePeers)
        {
            if (peer.Torrent == null || peer.User == null) continue;

            var torrentSizeGB = (double)peer.Torrent.Size / (1024 * 1024 * 1024);
            var seederCount = await dbContext.Peers.CountAsync(p => p.TorrentId == peer.TorrentId && p.IsSeeder);
            var leecherCount = await dbContext.Peers.CountAsync(p => p.TorrentId == peer.TorrentId && !p.IsSeeder);

            // Formula: BaseRate * (SizeFactor) * (LeecherFactor / SeederFactor)
            double points = _settings.BaseGenerationRate;
            points *= (1 + torrentSizeGB * _settings.SizeFactorMultiplier); // Size bonus
            points *= (1 + leecherCount * _settings.LeecherFactorMultiplier); // More leechers = more reward
            points /= (1 + (seederCount - 1) * _settings.SeederFactorMultiplier); // More seeders = less reward (for this user)

            if (!userCoinGains.ContainsKey(peer.UserId))
            {
                userCoinGains[peer.UserId] = 0;
            }
            userCoinGains[peer.UserId] += points;
        }

        if (userCoinGains.Any())
        {
            var userIds = userCoinGains.Keys.ToList();
            var usersToUpdate = await dbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

            foreach (var user in usersToUpdate)
            {
                var gain = (long)Math.Round(userCoinGains[user.Id]);
                user.SakuraCoins += gain;
                _logger.LogDebug("User {UserId} earned {Coins} SakuraCoins this cycle.", user.Id, gain);
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Successfully updated SakuraCoins for {UserCount} users.", usersToUpdate.Count);
        }
        else
        {
            _logger.LogInformation("No active seeders found. No coins generated this cycle.");
        }
    }
}
