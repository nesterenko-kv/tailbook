using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Telemetry;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence;

public sealed class DomainEventToOutboxInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StageDomainEvents(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StageDomainEvents(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void StageDomainEvents(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        var domainEventEntries = dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(x => x.Entity.GetDomainEvents().Count > 0)
            .ToArray();

        foreach (var entry in domainEventEntries)
        {
            foreach (var domainEvent in entry.Entity.GetDomainEvents())
            {
                var payload = OutboxPayloadProjector.Project(domainEvent);
                var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
                var payloadSizeBytes = System.Text.Encoding.UTF8.GetByteCount(payloadJson);

                using var activity = OutboxTelemetry.StartMessageStagedActivity(
                    domainEvent.ModuleCode,
                    domainEvent.EventType,
                    domainEvent.EventId,
                    payloadSizeBytes);

                dbContext.Set<OutboxMessage>().Add(new OutboxMessage
                {
                    Id = domainEvent.EventId,
                    ModuleCode = domainEvent.ModuleCode,
                    EventType = domainEvent.EventType,
                    PayloadJson = payloadJson,
                    OccurredAt = domainEvent.OccurredAt,
                    ProcessedAt = null
                });

                OutboxTelemetry.RecordMessageStaged(domainEvent.ModuleCode, domainEvent.EventType, payloadSizeBytes);
            }

            entry.Entity.ClearDomainEvents();
        }
    }
}
