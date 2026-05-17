using Microsoft.Extensions.Logging;

namespace Tailbook.Modules.VisitOperations.Infrastructure.BackgroundJobs;

internal static partial class VisitCancellationConsumerLogExtensions
{
    [LoggerMessage(1, LogLevel.Information, "VisitCancellationConsumer started. Queue: {Queue}, Exchange: {Exchange}, RoutingKey: {RoutingKey}.")]
    public static partial void VisitCancellationConsumerStarted(this ILogger logger, string queue, string exchange, string routingKey);

    [LoggerMessage(2, LogLevel.Information, "VisitCancellationConsumer stopped.")]
    public static partial void VisitCancellationConsumerStopped(this ILogger logger);

    [LoggerMessage(3, LogLevel.Debug, "Appointment {AppointmentId} cancelled — no active visit found. MessageId: {MessageId}.")]
    public static partial void AppointmentCancellationNoVisit(this ILogger logger, Guid messageId, Guid appointmentId);

    [LoggerMessage(4, LogLevel.Information, "Processing cancellation for appointment {AppointmentId}, visit {VisitId} (status: {Status}). MessageId: {MessageId}.")]
    public static partial void AppointmentCancellationProcessing(this ILogger logger, Guid messageId, Guid appointmentId, Guid visitId, string status);
}
