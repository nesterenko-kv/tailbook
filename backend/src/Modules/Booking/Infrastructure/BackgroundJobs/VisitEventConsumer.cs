using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;

namespace Tailbook.Modules.Booking.Infrastructure.BackgroundJobs;

public sealed class VisitEventConsumer : IntegrationEventConsumerBase
{
    private readonly ILogger<VisitEventConsumer> _logger;

    protected override string QueueName => "booking-visit-events";
    protected override string[] RoutingKeys =>
    [
        "visitops.visit-checked-in",
        "visitops.visit-completed",
        "visitops.visit-closed"
    ];

    public VisitEventConsumer(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<VisitEventConsumer> logger
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

        var appointmentId = payload.TryGetProperty("appointmentId", out var aid) ? aid.GetGuid() : (Guid?)null;

        if (appointmentId is null || appointmentId == Guid.Empty)
        {
            _logger.LogWarning("Visit event {EventType} missing appointmentId from {RoutingKey}.", eventType, routingKey);
            return;
        }

        using var scope = ScopeFactory.CreateScope();
        var appointmentVisitService = scope.ServiceProvider.GetRequiredService<IAppointmentVisitService>();

        _logger.VisitEventProcessing(messageId, eventType, routingKey, appointmentId.Value);

        switch (eventType)
        {
            case "VisitCheckedIn":
            {
                var result = await appointmentVisitService.MarkCheckedInAsync(appointmentId.Value, null, cancellationToken);
                if (result.IsError)
                {
                    _logger.LogWarning("Failed to mark appointment {AppointmentId} as checked in: {Error}", appointmentId.Value, result.FirstError.Description);
                }
                break;
            }
            case "VisitCompleted":
            {
                var result = await appointmentVisitService.MarkCompletedAsync(appointmentId.Value, null, cancellationToken);
                if (result.IsError)
                {
                    _logger.LogWarning("Failed to mark appointment {AppointmentId} as completed: {Error}", appointmentId.Value, result.FirstError.Description);
                }
                break;
            }
            case "VisitClosed":
            {
                var result = await appointmentVisitService.MarkClosedAsync(appointmentId.Value, null, cancellationToken);
                if (result.IsError)
                {
                    _logger.LogWarning("Failed to mark appointment {AppointmentId} as closed: {Error}", appointmentId.Value, result.FirstError.Description);
                }
                break;
            }
            default:
                _logger.LogDebug("Ignoring unknown visit event type {EventType} from {RoutingKey}.", eventType, routingKey);
                break;
        }
    }
}
