using Microsoft.Extensions.Logging;

namespace Tailbook.Modules.Booking.Infrastructure.BackgroundJobs;

internal static partial class VisitEventConsumerLogExtensions
{
    [LoggerMessage(1, LogLevel.Information, "VisitEventConsumer started. Queue: {Queue}, Exchange: {Exchange}, Bindings: {BindingCount}.")]
    public static partial void VisitEventConsumerStarted(this ILogger logger, string queue, string exchange, int bindingCount);

    [LoggerMessage(2, LogLevel.Information, "VisitEventConsumer stopped.")]
    public static partial void VisitEventConsumerStopped(this ILogger logger);

    [LoggerMessage(3, LogLevel.Debug, "Visit event {MessageId} ({EventType}) received from {RoutingKey}. AppointmentId: {AppointmentId}.")]
    public static partial void VisitEventProcessing(this ILogger logger, Guid messageId, string eventType, string routingKey, Guid appointmentId);
}
