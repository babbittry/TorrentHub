using TorrentHub.Core.Services;

namespace TorrentHub.Services.Background
{
    public class RssFeedTokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RssFeedTokenCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // 每24小时运行一次

        public RssFeedTokenCleanupService(
            IServiceProvider serviceProvider,
            ILogger<RssFeedTokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RSS Feed Token Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredTokensAsync();
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up expired RSS feed tokens");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // 出错后等待5分钟再重试
                }
            }

            _logger.LogInformation("RSS Feed Token Cleanup Service stopped");
        }

        private async Task CleanupExpiredTokensAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var tokenService = scope.ServiceProvider.GetRequiredService<IRssFeedTokenService>();

            _logger.LogInformation("Starting cleanup of expired RSS feed tokens");

            try
            {
                await tokenService.CleanupExpiredTokensAsync();
                _logger.LogInformation("Successfully cleaned up expired RSS feed tokens");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired RSS feed tokens");
                throw;
            }
        }
    }
}