using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            logger.LogInformation("Notifications background processing is disabled.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(5, currentOptions.BackgroundPollIntervalSeconds));
        logger.LogInformation("Notifications background processing started with poll interval {IntervalSeconds}s.", interval.TotalSeconds);

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
                logger.LogError(ex, "Failed to process outbox in background worker.");
            }
        }
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<NotificationQueries>();
        var processed = await queries.ProcessOutboxAsync(cancellationToken);
        if (processed > 0)
        {
            logger.LogInformation("Background outbox processor handled {ProcessedCount} message(s).", processed);
        }
    }
}
