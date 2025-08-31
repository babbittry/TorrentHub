using Microsoft.Extensions.DependencyInjection;

namespace TorrentHub.Services.Background;
using TorrentHub.Services.Interfaces;

public class TopPlayersCacheRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TopPlayersCacheRefreshService> _logger;

    public TopPlayersCacheRefreshService(IServiceProvider serviceProvider, ILogger<TopPlayersCacheRefreshService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Top Players Cache Refresh Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting top players cache refresh cycle.");
                using (var scope = _serviceProvider.CreateScope())
                {
                    var topPlayersService = scope.ServiceProvider.GetRequiredService<ITopPlayersService>();
                    await topPlayersService.RefreshTopPlayersCacheAsync();
                }
                _logger.LogInformation("Top players cache refresh cycle finished.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during top players cache refresh cycle.");
            }

            // Refresh every 1 hour (adjust as needed)
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        _logger.LogInformation("Top Players Cache Refresh Service is stopping.");
    }
}
