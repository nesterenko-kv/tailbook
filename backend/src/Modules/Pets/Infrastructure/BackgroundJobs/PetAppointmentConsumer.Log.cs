using Microsoft.Extensions.Logging;

namespace Tailbook.Modules.Pets.Infrastructure.BackgroundJobs;

internal static partial class PetAppointmentConsumerLogExtensions
{
    [LoggerMessage(1, LogLevel.Information, "PetAppointmentConsumer started. Queue: {Queue}, Exchange: {Exchange}, Bindings: {BindingCount}.")]
    public static partial void PetAppointmentConsumerStarted(this ILogger logger, string queue, string exchange, int bindingCount);

    [LoggerMessage(2, LogLevel.Information, "PetAppointmentConsumer stopped.")]
    public static partial void PetAppointmentConsumerStopped(this ILogger logger);

    [LoggerMessage(3, LogLevel.Debug, "Pet appointment event {MessageId} ({EventType}) received from routing key {RoutingKey}.")]
    public static partial void PetAppointmentEventReceived(this ILogger logger, Guid messageId, string eventType, string routingKey);
}
