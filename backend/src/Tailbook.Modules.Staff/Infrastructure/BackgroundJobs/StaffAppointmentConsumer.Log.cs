using Microsoft.Extensions.Logging;

namespace Tailbook.Modules.Staff.Infrastructure.BackgroundJobs;

internal static partial class StaffAppointmentConsumerLog
{
    [LoggerMessage(
        EventId = 9200,
        Level = LogLevel.Information,
        Message = "Staff appointment consumer started. Listening on queue {Queue} (exchange {Exchange}, {BindingCount} bindings).")]
    public static partial void StaffAppointmentConsumerStarted(this ILogger logger, string queue, string exchange, int bindingCount);

    [LoggerMessage(
        EventId = 9201,
        Level = LogLevel.Information,
        Message = "Staff appointment consumer stopped.")]
    public static partial void StaffAppointmentConsumerStopped(this ILogger logger);

    [LoggerMessage(
        EventId = 9202,
        Level = LogLevel.Debug,
        Message = "Appointment event {MessageId} ({EventType}) received from {RoutingKey}.")]
    public static partial void AppointmentEventReceived(this ILogger logger, Guid messageId, string eventType, string routingKey);
}
