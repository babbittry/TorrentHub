using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TorrentHub.Core.Services;
using TorrentHub.Services.Configuration;

namespace TorrentHub.Services.Background;

/// <summary>
/// 后台服务,定期清理长期未使用的inactive credentials
/// </summary>
public class CredentialCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CredentialCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;

    public CredentialCleanupService(
        IServiceProvider serviceProvider,
        IOptions<CredentialSettings> credentialSettings,
        ILogger<CredentialCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _cleanupInterval = TimeSpan.FromDays(credentialSettings.Value.CleanupIntervalDays);
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
            var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();

            // 从数据库读取业务规则配置
            var siteSettings = await settingsService.GetSiteSettingsAsync();
            
            if (!siteSettings.EnableCredentialAutoCleanup)
            {
                _logger.LogInformation("Credential auto-cleanup is disabled in site settings");
                return;
            }

            var inactiveDays = siteSettings.CredentialCleanupDays;
            var count = await credentialService.CleanupInactiveCredentialsAsync(inactiveDays);

            if (count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} inactive credentials (inactive for {Days} days)", count, inactiveDays);
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