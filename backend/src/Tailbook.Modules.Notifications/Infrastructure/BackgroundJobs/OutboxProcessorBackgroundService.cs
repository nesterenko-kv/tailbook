using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.Modules.Notifications.Infrastructure.Options;
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
                await PublishPendingToBrokerAsync(stoppingToken);
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

    private async Task PublishPendingToBrokerAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var broker = scope.ServiceProvider.GetRequiredService<IMessageBroker>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var exchange = "tailbook.events";

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.OccurredAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        var utcNow = timeProvider.GetUtcNow();

        foreach (var message in messages)
        {
            try
            {
                var routingKey = ToRoutingKey(message.ModuleCode, message.EventType);

                await broker.PublishAsync(
                    exchange,
                    routingKey,
                    new { message.EventType, message.PayloadJson, MessageId = message.Id },
                    messageId: message.Id.ToString("D"),
                    cancellationToken);

                message.ProcessedAt = utcNow;
            }
            catch (Exception ex)
            {
                logger.OutboxPublishFailed(message.Id, message.EventType, ex);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.BackgroundOutboxProcessorHandled(messages.Count);
    }

    private static string ToRoutingKey(string moduleCode, string eventType)
    {
        var kebab = string.Concat(
            eventType.Select((c, i) =>
                i > 0 && char.IsUpper(c)
                    ? "-" + char.ToLowerInvariant(c)
                    : char.ToLowerInvariant(c).ToString()));
        return $"{moduleCode}.{kebab}";
    }
}
