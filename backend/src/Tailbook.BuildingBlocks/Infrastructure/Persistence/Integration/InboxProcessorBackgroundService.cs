using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Telemetry;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

public sealed class InboxProcessorBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<InboxOptions> options,
    ILogger<InboxProcessorBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var currentOptions = options.Value;
        if (!currentOptions.EnableBackgroundProcessing)
        {
            logger.LogInformation("Inbox background processing is disabled.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(5, currentOptions.PollIntervalSeconds));
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Inbox background processing started with poll interval {Interval}s.", interval.TotalSeconds);
        }

        using var timer = new PeriodicTimer(interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessInboxMessagesAsync(currentOptions, stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process inbox messages in background worker.");
            }
        }
    }

    private async Task ProcessInboxMessagesAsync(InboxOptions currentOptions, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var handlers = scope.ServiceProvider.GetRequiredService<IEnumerable<IInboxMessageHandler>>();
        var cache = scope.ServiceProvider.GetService<IDistributedCache>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var utcNow = timeProvider.GetUtcNow();

        var messages = await dbContext.Set<InboxMessage>()
            .Where(x => (x.Status == "Received" || x.Status == "Failed") && !x.IsPoisoned)
            .Where(x => x.NextRetryAt == null || x.NextRetryAt <= utcNow)
            .OrderBy(x => x.ReceivedAt)
            .Take(currentOptions.BatchSize)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            var handler = handlers.FirstOrDefault(h => h.ConsumerName == message.ConsumerName);
            if (handler == null)
            {
                message.Status = "Poisoned";
                message.IsPoisoned = true;
                message.PoisonedAt = utcNow;
                message.LastError = $"No handler registered for consumer '{message.ConsumerName}'.";
                InboxTelemetry.RecordPoisoned(message.ConsumerName);
                logger.LogWarning("Inbox message {MessageId} ({EventType}) poisoned: no handler for consumer {Consumer}.", message.MessageId, message.EventType, message.ConsumerName);
                continue;
            }

            message.Status = "Processing";

            try
            {
                await handler.HandleAsync(message.EventType, message.PayloadJson, Guid.Parse(message.MessageId), ct);

                message.Status = "Completed";
                message.ProcessedAt = utcNow;
                InboxTelemetry.RecordCompleted(message.ConsumerName);

                if (cache != null)
                {
                    var cacheKey = CacheKeys.InboxMessage(message.MessageId, message.ConsumerName);
                    await cache.SetStringAsync(cacheKey, "Completed", new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
                    }, ct);
                }
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.LastError = ex.Message;

                if (message.RetryCount >= currentOptions.MaxRetryAttempts)
                {
                    message.Status = "Poisoned";
                    message.IsPoisoned = true;
                    message.PoisonedAt = utcNow;
                    InboxTelemetry.RecordPoisoned(message.ConsumerName);
                    logger.LogWarning(ex, "Inbox message {MessageId} ({EventType}) moved to poisoned state after {RetryCount} failed attempts.", message.MessageId, message.EventType, message.RetryCount);
                }
                else
                {
                    message.Status = "Failed";
                    var backoffSeconds = currentOptions.BackoffBaseDelaySeconds * Math.Pow(2, message.RetryCount);
                    message.NextRetryAt = message.ReceivedAt.AddSeconds(backoffSeconds);
                    InboxTelemetry.RecordFailed(message.ConsumerName);
                    InboxTelemetry.RecordRetryDepth(message.ConsumerName, message.RetryCount);
                    logger.LogWarning(ex, "Inbox message {MessageId} ({EventType}) processing failed, retry {RetryCount}/{MaxRetryAttempts}. Next retry at {NextRetryAt}. Error: {LastError}", message.MessageId, message.EventType, message.RetryCount, currentOptions.MaxRetryAttempts, message.NextRetryAt, message.LastError);
                }
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(ct);
            logger.LogDebug("Inbox processor handled {Count} messages.", messages.Count);
        }
    }
}
