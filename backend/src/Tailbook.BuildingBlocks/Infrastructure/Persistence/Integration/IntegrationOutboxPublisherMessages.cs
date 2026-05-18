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
}
