using Microsoft.EntityFrameworkCore;
using TorrentHub.Data;
using TorrentHub.DTOs;
using TorrentHub.Services;

namespace TorrentHub.Services;

public class CoinGenerationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CoinGenerationService> _logger;

    public CoinGenerationService(
        IServiceProvider serviceProvider,
        ILogger<CoinGenerationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Coin Generation Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Create a scope to resolve scoped services like DbContext and SettingsService
            using var scope = _serviceProvider.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
            var settings = await settingsService.GetSiteSettingsAsync();

            try
            {
                _logger.LogInformation("Starting Coin generation cycle.");
                await GenerateCoinsAsync(scope, settings);
                _logger.LogInformation("Coin generation cycle finished. Waiting for the next cycle.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the Coin generation cycle.");
            }

            // Use the generation interval from the dynamic settings
            var intervalMinutes = settings.GenerationIntervalMinutes > 0 ? settings.GenerationIntervalMinutes : 60;
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Coin Generation Service is stopping.");
    }

    private async Task GenerateCoinsAsync(IServiceScope scope, SiteSettingsDto settings)
    {
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
            var mosquitoCount = await dbContext.Peers.CountAsync(p => p.TorrentId == peer.TorrentId && !p.IsSeeder);

            double points = settings.BaseGenerationRate;
            points *= (1 + torrentSizeGB * settings.SizeFactorMultiplier);
            points *= (1 + mosquitoCount * settings.MosquitoFactorMultiplier);
            points /= (1 + (seederCount - 1) * settings.SeederFactorMultiplier);

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
                var gain = (ulong)Math.Round(userCoinGains[user.Id]);
                user.Coins += gain;
                _logger.LogDebug("User {UserId} earned {Coins} Coins this cycle.", user.Id, gain);
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Successfully updated Coins for {UserCount} users.", usersToUpdate.Count);
        }
        else
        {
            _logger.LogInformation("No active seeders found. No coins generated this cycle.");
        }
    }
}