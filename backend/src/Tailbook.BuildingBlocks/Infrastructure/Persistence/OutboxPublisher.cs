using System.Text;
using System.Text.Json;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Telemetry;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence;

public sealed class OutboxPublisher(AppDbContext dbContext, TimeProvider timeProvider) : IOutboxPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task PublishAsync(string moduleCode, string eventType, object payload, CancellationToken cancellationToken)
    {
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var messageId = Guid.NewGuid();
        var payloadSizeBytes = Encoding.UTF8.GetByteCount(payloadJson);
        using var activity = OutboxTelemetry.StartMessageStagedActivity(moduleCode, eventType, messageId, payloadSizeBytes);

        dbContext.Set<OutboxMessage>().Add(new OutboxMessage
        {
            Id = messageId,
            ModuleCode = moduleCode,
            EventType = eventType,
            PayloadJson = payloadJson,
            OccurredAt = timeProvider.GetUtcNow(),
            ProcessedAt = null
        });
        OutboxTelemetry.RecordMessageStaged(moduleCode, eventType, payloadSizeBytes);

        return Task.CompletedTask;
    }
}
