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

    /// <summary>
    /// Generates SakuraCoins for active seeders based on predefined rules.
    /// This method is executed periodically by the background service.
    /// </summary>
    private async Task GenerateCoinsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 1. Retrieve all currently active seeders.
        // We include Torrent and User navigation properties to access their data for calculations.
        var activePeers = await dbContext.Peers
            .Where(p => p.IsSeeder) // Only consider seeders for coin generation
            .Include(p => p.Torrent)
            .Include(p => p.User)
            .ToListAsync();

        // A dictionary to accumulate total coin gains for each user across all their seeding torrents.
        var userCoinGains = new Dictionary<int, double>();

        // 2. Calculate points for each individual seeding peer.
        foreach (var peer in activePeers)
        {
            // Ensure torrent and user data are available before proceeding.
            if (peer.Torrent == null || peer.User == null) continue;

            // Convert torrent size to GB for easier calculation.
            var torrentSizeGB = (double)peer.Torrent.Size / (1024 * 1024 * 1024);

            // Get the current number of seeders and Mosquitoes for the specific torrent.
            // This helps determine the "hotness" or "rarity" of the torrent.
            var seederCount = await dbContext.Peers.CountAsync(p => p.TorrentId == peer.TorrentId && p.IsSeeder);
            var mosquitoCount = await dbContext.Peers.CountAsync(p => p.TorrentId == peer.TorrentId && !p.IsSeeder);

            // 3. Apply the SakuraCoin generation formula.
            // The formula aims to reward:
            // - More for larger torrents (SizeFactorMultiplier).
            // - More for torrents with more Mosquitoes (higher demand, MosquitoFactorMultiplier).
            // - Less for torrents with many seeders (less critical to seed, SeederFactorMultiplier).
            // The (seederCount - 1) is used to avoid division by zero if there's only one seeder (the current user).
            double points = _settings.BaseGenerationRate;
            points *= (1 + torrentSizeGB * _settings.SizeFactorMultiplier); // Bonus for seeding larger torrents.
            points *= (1 + mosquitoCount * _settings.MosquitoFactorMultiplier); // Bonus for seeding torrents with demand.
            points /= (1 + (seederCount - 1) * _settings.SeederFactorMultiplier); // Penalty for seeding overly popular torrents (less impact per seeder).

            // Accumulate points for the user. A user might be seeding multiple torrents.
            if (!userCoinGains.ContainsKey(peer.UserId))
            {
                userCoinGains[peer.UserId] = 0;
            }
            userCoinGains[peer.UserId] += points;
        }

        // 4. Update each user's total SakuraCoins.
        if (userCoinGains.Any())
        {
            // Get all users who earned coins in this cycle to update them in a batch.
            var userIds = userCoinGains.Keys.ToList();
            var usersToUpdate = await dbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

            foreach (var user in usersToUpdate)
            {
                // Round the accumulated double points to a long integer for the final balance.
                var gain = (long)Math.Round(userCoinGains[user.Id]);
                user.SakuraCoins += gain;
                _logger.LogDebug("User {UserId} earned {Coins} SakuraCoins this cycle.", user.Id, gain);
            }

            // Save all changes to the database.
            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Successfully updated SakuraCoins for {UserCount} users.", usersToUpdate.Count);
        }
        else
        {
            _logger.LogInformation("No active seeders found. No coins generated this cycle.");
        }
    }
}
