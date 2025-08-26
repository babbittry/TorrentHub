
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TorrentHub.Services;

public class RequestConfirmationService : BackgroundService
{
    private readonly ILogger<RequestConfirmationService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public RequestConfirmationService(ILogger<RequestConfirmationService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Request Confirmation Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Request Confirmation Service is running.");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var requestService = scope.ServiceProvider.GetRequiredService<IRequestService>();
                    await requestService.AutoCompleteExpiredConfirmationsAsync();
                }

                // Run once every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in Request Confirmation Service.");
                // Wait 5 minutes before retrying in case of a transient error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Request Confirmation Service is stopping.");
    }
}
