using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.VisitOperations.Domain.Aggregates;
using static Tailbook.Modules.VisitOperations.Domain.VisitStatusCodes;

namespace Tailbook.Modules.VisitOperations.Infrastructure.BackgroundJobs;

public sealed class VisitCancellationConsumer : IntegrationEventConsumerBase
{
    private readonly ILogger<VisitCancellationConsumer> _logger;
    private readonly TimeProvider _timeProvider;

    protected override string QueueName => "visitops-cancellations";
    protected override string[] RoutingKeys => ["booking.appointment-cancelled"];

    public VisitCancellationConsumer(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<VisitCancellationConsumer> logger,
        TimeProvider timeProvider
    ) : base(connectionFactory, rabbitMqOptions, scopeFactory, logger)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ProcessEventAsync(
        string eventType,
        string innerPayload,
        Guid messageId,
        string routingKey,
        CancellationToken cancellationToken)
    {
        using var payloadDoc = JsonDocument.Parse(innerPayload);
        var payload = payloadDoc.RootElement;

        var appointmentId = payload.TryGetProperty("appointmentId", out var aid) ? aid.GetGuid() : (Guid?)null;
        if (appointmentId is null || appointmentId == Guid.Empty)
        {
            _logger.LogWarning("Cancellation event missing appointmentId from {RoutingKey}.", routingKey);
            return;
        }

        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var visit = await dbContext.Set<Visit>()
            .Where(v => v.AppointmentId == appointmentId.Value)
            .Where(v => v.Status == Open || v.Status == InProgress)
            .FirstOrDefaultAsync(cancellationToken);

        if (visit is null)
        {
            _logger.AppointmentCancellationNoVisit(messageId, appointmentId.Value);
            return;
        }

        _logger.AppointmentCancellationProcessing(messageId, appointmentId.Value, visit.Id, visit.Status);

        var cancelResult = visit.Cancel(null, null, _timeProvider.GetUtcNow());
        if (cancelResult.IsError)
        {
            _logger.LogWarning("Failed to cancel visit {VisitId}: {Error}", visit.Id, cancelResult.FirstError.Description);
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.VisitCancelled(messageId, appointmentId.Value, visit.Id);
    }
}
