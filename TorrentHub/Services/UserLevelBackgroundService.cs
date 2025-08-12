using Microsoft.Extensions.DependencyInjection;

namespace TorrentHub.Services;

public class UserLevelBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserLevelBackgroundService> _logger;

    public UserLevelBackgroundService(IServiceProvider serviceProvider, ILogger<UserLevelBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("User Level Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting user level check cycle.");
                using (var scope = _serviceProvider.CreateScope())
                {
                    var userLevelService = scope.ServiceProvider.GetRequiredService<IUserLevelService>();
                    await userLevelService.CheckAndPromoteDemoteUsersAsync();
                }
                _logger.LogInformation("User level check cycle finished.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during user level check cycle.");
            }

            // Run every 1 hour (adjust as needed)
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        _logger.LogInformation("User Level Background Service is stopping.");
    }
}
