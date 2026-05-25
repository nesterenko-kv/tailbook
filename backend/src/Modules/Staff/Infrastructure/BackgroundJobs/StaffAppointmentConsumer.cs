using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;

namespace Tailbook.Modules.Staff.Infrastructure.BackgroundJobs;

public sealed class StaffAppointmentConsumer : IntegrationEventConsumerBase
{
    private readonly ILogger<StaffAppointmentConsumer> _logger;

    protected override string QueueName => "staff-appointments";
    protected override string[] RoutingKeys =>
    [
        "booking.appointment-created",
        "booking.appointment-cancelled",
        "booking.appointment-rescheduled"
    ];

    public StaffAppointmentConsumer(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<StaffAppointmentConsumer> logger
    ) : base(connectionFactory, rabbitMqOptions, scopeFactory, logger)
    {
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(
        string eventType,
        string innerPayload,
        Guid messageId,
        string routingKey,
        CancellationToken cancellationToken)
    {
        using var innerDoc = JsonDocument.Parse(innerPayload);
        var payload = innerDoc.RootElement;

        var groomerId = payload.TryGetProperty("groomerId", out var gid) ? gid.GetGuid() : (Guid?)null;
        var appointmentId = payload.TryGetProperty("appointmentId", out var aid) ? aid.GetGuid() : (Guid?)null;

        _logger.AppointmentEventReceived(messageId, eventType, routingKey, groomerId ?? Guid.Empty, appointmentId ?? Guid.Empty);
    }
}
