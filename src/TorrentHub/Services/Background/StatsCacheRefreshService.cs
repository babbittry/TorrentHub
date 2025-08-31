namespace TorrentHub.Services.Background;
using TorrentHub.Services.Interfaces;

public class StatsCacheRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StatsCacheRefreshService> _logger;

    public StatsCacheRefreshService(IServiceProvider serviceProvider, ILogger<StatsCacheRefreshService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Site Stats Cache Refresh Service is starting.");

        // Wait a little before the first run to ensure other services are ready
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting site stats cache refresh cycle.");
                using (var scope = _serviceProvider.CreateScope())
                {
                    var statsService = scope.ServiceProvider.GetRequiredService<IStatsService>();
                    await statsService.RecalculateAndCacheSiteStatsAsync();
                }
                _logger.LogInformation("Site stats cache refresh cycle finished.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during site stats cache refresh cycle.");
            }

            // Refresh every 30 minutes
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }

        _logger.LogInformation("Site Stats Cache Refresh Service is stopping.");
    }
}
