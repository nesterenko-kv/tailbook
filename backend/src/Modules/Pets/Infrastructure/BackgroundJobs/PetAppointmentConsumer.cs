using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;

namespace Tailbook.Modules.Pets.Infrastructure.BackgroundJobs;

public sealed class PetAppointmentConsumer(
    RabbitMqConnectionFactory connectionFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<PetAppointmentConsumer> logger)
    : IntegrationEventConsumerBase(connectionFactory, rabbitMqOptions, scopeFactory, logger)
{
    protected override string QueueName => "pet-appointments";
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
        var payload = innerDoc.RootElement;

        var petId = payload.TryGetProperty("petId", out var pid) ? pid.GetGuid() : (Guid?)null;
        var appointmentId = payload.TryGetProperty("appointmentId", out var aid) ? aid.GetGuid() : (Guid?)null;

        logger.PetAppointmentEventReceived(messageId, eventType, routingKey, petId ?? Guid.Empty, appointmentId ?? Guid.Empty);
    }
}
