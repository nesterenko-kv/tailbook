using Microsoft.Extensions.Logging;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

internal static partial class IntegrationOutboxPublisherMessages
{
    [LoggerMessage(
        EventId = 9000,
        Level = LogLevel.Information,
        Message = "Integration outbox background publishing is disabled.")]
    public static partial void IntegrationOutboxPublishingDisabled(this ILogger logger);

    [LoggerMessage(
        EventId = 9001,
        Level = LogLevel.Information,
        Message = "Integration outbox background publishing started with poll interval {IntervalSeconds}s.")]
    public static partial void IntegrationOutboxPublishingStarted(this ILogger logger, double intervalSeconds);

    [LoggerMessage(
        EventId = 9002,
        Level = LogLevel.Error,
        Message = "Failed to publish integration outbox messages in background worker.")]
    public static partial void IntegrationOutboxPublishingFailed(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 9003,
        Level = LogLevel.Information,
        Message = "Integration outbox publisher handled {ProcessedCount} messages.")]
    public static partial void IntegrationOutboxPublisherHandled(this ILogger logger, int processedCount);

    [LoggerMessage(
        EventId = 9004,
        Level = LogLevel.Error,
        Message = "Failed to publish integration outbox message {MessageId} ({EventType}) to broker.")]
    public static partial void IntegrationOutboxMessagePublishFailed(this ILogger logger, Guid messageId, string eventType, Exception exception);

    [LoggerMessage(
        EventId = 9005,
        Level = LogLevel.Warning,
        Message = "Integration outbox message {MessageId} ({EventType}) publish failed, retry {RetryCount}/{MaxRetryAttempts}. Next retry at {NextRetryAt}. Error: {LastError}")]
    public static partial void IntegrationOutboxMessageRetrying(this ILogger logger, Guid messageId, string eventType, int retryCount, int maxRetryAttempts, DateTimeOffset? nextRetryAt, string? lastError);

    [LoggerMessage(
        EventId = 9006,
        Level = LogLevel.Error,
        Message = "Integration outbox message {MessageId} ({EventType}) moved to poisoned state after {RetryCount} failed attempts.")]
    public static partial void IntegrationOutboxMessagePoisoned(this ILogger logger, Guid messageId, string eventType, int retryCount);

    [LoggerMessage(
        EventId = 9007,
        Level = LogLevel.Warning,
        Message = "Found {StuckCount} stuck integration outbox messages that require operator review.")]
    public static partial void IntegrationOutboxStuckMessagesFound(this ILogger logger, int stuckCount);

    [LoggerMessage(
        EventId = 9008,
        Level = LogLevel.Information,
        Message = "Integration outbox message {MessageId} ({EventType}) published successfully.")]
    public static partial void IntegrationOutboxMessagePublished(this ILogger logger, Guid messageId, string eventType);

    [LoggerMessage(
        EventId = 9009,
        Level = LogLevel.Warning,
        Message = "Poison outbox monitor found {PoisonedCount} poisoned messages.")]
    public static partial void PoisonOutboxMessagesFound(this ILogger logger, int poisonedCount);
}
