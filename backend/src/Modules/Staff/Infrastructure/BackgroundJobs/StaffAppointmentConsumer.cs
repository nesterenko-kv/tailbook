using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;

namespace Tailbook.Modules.Staff.Infrastructure.BackgroundJobs;

public sealed class StaffAppointmentConsumer(
    RabbitMqConnectionFactory connectionFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<StaffAppointmentConsumer> logger)
    : IntegrationEventConsumerBase(connectionFactory, rabbitMqOptions, scopeFactory, logger)
{
    protected override string QueueName => "staff-appointments";

    protected override string[] RoutingKeys =>
    [
        "booking.appointment-created",
        "booking.appointment-cancelled",
        "booking.appointment-rescheduled"
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

        Guid? groomerId = payload.TryGetProperty("groomerId", out JsonElement gid) ? gid.GetGuid() : null;
        Guid? appointmentId = payload.TryGetProperty("appointmentId", out JsonElement aid) ? aid.GetGuid() : null;

        logger.AppointmentEventReceived(messageId, eventType, routingKey, groomerId ?? Guid.Empty,
            appointmentId ?? Guid.Empty);
    }
}
