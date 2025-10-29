using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TorrentHub.Core.Services;

namespace TorrentHub.Services.Background;

/// <summary>
/// 后台服务,定期清理长期未使用的inactive credentials
/// </summary>
public class CredentialCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CredentialCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromDays(1); // 每天运行一次
    private readonly int _inactiveDays = 90; // 90天未使用视为inactive

    public CredentialCleanupService(
        IServiceProvider serviceProvider,
        ILogger<CredentialCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Credential Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
                
                if (stoppingToken.IsCancellationRequested)
                    break;

                await CleanupInactiveCredentialsAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected when the service is stopping
                _logger.LogInformation("Credential Cleanup Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Credential Cleanup Service");
                // Continue running despite errors
            }
        }

        _logger.LogInformation("Credential Cleanup Service stopped");
    }

    private async Task CleanupInactiveCredentialsAsync()
    {
        _logger.LogInformation("Starting credential cleanup task");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var credentialService = scope.ServiceProvider.GetRequiredService<ITorrentCredentialService>();

            var count = await credentialService.CleanupInactiveCredentialsAsync(_inactiveDays);

            if (count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} inactive credentials (inactive for {Days} days)", count, _inactiveDays);
            }
            else
            {
                _logger.LogInformation("No inactive credentials found to clean up");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during credential cleanup");
            throw;
        }
    }
}