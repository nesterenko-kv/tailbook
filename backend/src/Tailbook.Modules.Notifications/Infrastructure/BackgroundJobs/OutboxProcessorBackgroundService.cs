using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tailbook.Modules.Notifications.Infrastructure.Options;
using Tailbook.Modules.Notifications.Infrastructure.Services;
using Tailbook.Modules.Notifications.Infrastructure.Telemetry;

namespace Tailbook.Modules.Notifications.Infrastructure.BackgroundJobs;

public sealed class OutboxProcessorBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationsOptions> options,
    ILogger<OutboxProcessorBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var currentOptions = options.Value;
        if (!currentOptions.EnableBackgroundProcessing)
        {
            logger.NotificationsBackgroundProcessingDisabled();
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(5, currentOptions.BackgroundPollIntervalSeconds));
        if (logger.IsEnabled(LogLevel.Information))
        {
            var intervalSeconds = interval.TotalSeconds;
            logger.NotificationsBackgroundProcessingStarted(intervalSeconds);
        }

        using var timer = new PeriodicTimer(interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                NotificationTelemetry.RecordBackgroundProcessingFailure();
                logger.OutboxProcessingFailed(ex);
            }
        }
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var useCases = scope.ServiceProvider.GetRequiredService<NotificationUseCases>();
        var processed = await useCases.ProcessOutboxAsync(NotificationTelemetry.TriggerBackground, cancellationToken);
        if (processed > 0)
        {
            logger.BackgroundOutboxProcessorHandled(processed);
        }
    }
}
