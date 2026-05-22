using Microsoft.Extensions.Logging;

namespace Tailbook.Modules.Audit.Infrastructure.BackgroundJobs;

internal static partial class AuditEventConsumerLogExtensions
{
    [LoggerMessage(1, LogLevel.Information, "AuditEventConsumer started. Queue: {Queue}, Exchange: {Exchange}.")]
    public static partial void AuditEventConsumerStarted(this ILogger logger, string queue, string exchange);

    [LoggerMessage(2, LogLevel.Information, "AuditEventConsumer stopped.")]
    public static partial void AuditEventConsumerStopped(this ILogger logger);

    [LoggerMessage(3, LogLevel.Debug, "Audit event {MessageId} ({EventType}) processed from routing key {RoutingKey}.")]
    public static partial void AuditEventProcessed(this ILogger logger, Guid messageId, string eventType, string routingKey);
}
