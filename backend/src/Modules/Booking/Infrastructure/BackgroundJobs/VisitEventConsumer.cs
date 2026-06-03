using System.Text.Json;
using ErrorOr;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;

namespace Tailbook.Modules.Booking.Infrastructure.BackgroundJobs;

public sealed class VisitEventConsumer(
    RabbitMqConnectionFactory connectionFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<VisitEventConsumer> logger)
    : IntegrationEventConsumerBase(connectionFactory, rabbitMqOptions, scopeFactory, logger)
{
    protected override string QueueName => "booking-visit-events";

    protected override string[] RoutingKeys =>
    [
        "visitops.visit-checked-in",
        "visitops.visit-completed",
        "visitops.visit-closed"
    ];

    protected override async Task ProcessEventAsync(
        string eventType,
        string innerPayload,
        Guid messageId,
        string routingKey,
        CancellationToken cancellationToken)
    {
        using var innerDoc = JsonDocument.Parse(innerPayload);
        JsonElement payload = innerDoc.RootElement;

        Guid? appointmentId = payload.TryGetProperty("appointmentId", out JsonElement aid) ? aid.GetGuid() : null;

        if (appointmentId is null || appointmentId == Guid.Empty)
        {
            logger.LogWarning("Visit event {EventType} missing appointmentId from {RoutingKey}.", eventType,
                routingKey);
            return;
        }

        using IServiceScope scope = ScopeFactory.CreateScope();
        IAppointmentVisitService appointmentVisitService =
            scope.ServiceProvider.GetRequiredService<IAppointmentVisitService>();

        logger.VisitEventProcessing(messageId, eventType, routingKey, appointmentId.Value);

        switch (eventType)
        {
            case "VisitCheckedIn":
            {
                ErrorOr<Success> result =
                    await appointmentVisitService.MarkCheckedInAsync(appointmentId.Value, null, cancellationToken);
                if (result.IsError)
                    logger.LogWarning("Failed to mark appointment {AppointmentId} as checked in: {Error}",
                        appointmentId.Value, result.FirstError.Description);
                break;
            }
            case "VisitCompleted":
            {
                ErrorOr<Success> result =
                    await appointmentVisitService.MarkCompletedAsync(appointmentId.Value, null, cancellationToken);
                if (result.IsError)
                    logger.LogWarning("Failed to mark appointment {AppointmentId} as completed: {Error}",
                        appointmentId.Value, result.FirstError.Description);
                break;
            }
            case "VisitClosed":
            {
                ErrorOr<Success> result =
                    await appointmentVisitService.MarkClosedAsync(appointmentId.Value, null, cancellationToken);
                if (result.IsError)
                    logger.LogWarning("Failed to mark appointment {AppointmentId} as closed: {Error}",
                        appointmentId.Value, result.FirstError.Description);
                break;
            }
            default:
                logger.LogDebug("Ignoring unknown visit event type {EventType} from {RoutingKey}.", eventType,
                    routingKey);
                break;
        }
    }
}
