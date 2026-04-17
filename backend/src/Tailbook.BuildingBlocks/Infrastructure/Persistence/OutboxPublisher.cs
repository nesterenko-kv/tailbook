using System.Text.Json;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence;

public sealed class OutboxPublisher(AppDbContext dbContext) : IOutboxPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task PublishAsync(string moduleCode, string eventType, object payload, CancellationToken cancellationToken)
    {
        dbContext.Set<OutboxMessage>().Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            ModuleCode = moduleCode,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            OccurredAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = null
        });

        return Task.CompletedTask;
    }
}
