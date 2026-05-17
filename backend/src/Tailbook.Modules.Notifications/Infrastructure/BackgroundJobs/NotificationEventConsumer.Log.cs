using Microsoft.Extensions.Logging;

namespace Tailbook.Modules.Notifications.Infrastructure.BackgroundJobs;

internal static partial class NotificationEventConsumerLog
{
    [LoggerMessage(
        EventId = 9100,
        Level = LogLevel.Information,
        Message = "Notification consumer started. Listening on queue {Queue} (exchange {Exchange}).")]
    public static partial void NotificationConsumerStarted(this ILogger logger, string queue, string exchange);

    [LoggerMessage(
        EventId = 9101,
        Level = LogLevel.Information,
        Message = "Notification consumer stopped.")]
    public static partial void NotificationConsumerStopped(this ILogger logger);

    [LoggerMessage(
        EventId = 9102,
        Level = LogLevel.Debug,
        Message = "Notification event {MessageId} ({EventType}) sent successfully.")]
    public static partial void NotificationEventSent(this ILogger logger, Guid messageId, string eventType);
}
