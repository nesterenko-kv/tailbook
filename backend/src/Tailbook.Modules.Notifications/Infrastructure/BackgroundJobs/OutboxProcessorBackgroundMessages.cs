using Microsoft.Extensions.Logging;

namespace Tailbook.Modules.Notifications.Infrastructure.BackgroundJobs;

internal static partial class OutboxProcessorBackgroundMessages
{
    [LoggerMessage(
        EventId = 9000,
        Level = LogLevel.Information,
        Message = "Notifications background processing is disabled.")]
    public static partial void NotificationsBackgroundProcessingDisabled(this ILogger logger);

    [LoggerMessage(
        EventId = 9001,
        Level = LogLevel.Information,
        Message = "Notifications background processing started with poll interval {IntervalSeconds}s.")]
    public static partial void NotificationsBackgroundProcessingStarted(this ILogger logger, double intervalSeconds);

    [LoggerMessage(
        EventId = 9002,
        Level = LogLevel.Error,
        Message = "Failed to process outbox in background worker.")]
    public static partial void OutboxProcessingFailed(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 9003,
        Level = LogLevel.Information,
        Message = "Background outbox processor handled {ProcessedCount} messages.")]
    public static partial void BackgroundOutboxProcessorHandled(this ILogger logger, int processedCount);

    [LoggerMessage(
        EventId = 9004,
        Level = LogLevel.Error,
        Message = "Failed to publish outbox message {MessageId} ({EventType}) to broker.")]
    public static partial void OutboxPublishFailed(this ILogger logger, Guid messageId, string eventType, Exception exception);
}
