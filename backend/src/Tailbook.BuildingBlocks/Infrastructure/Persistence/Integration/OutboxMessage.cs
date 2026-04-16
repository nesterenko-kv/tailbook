using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }

    public static OutboxMessage From(string moduleCode, IIntegrationEvent integrationEvent, string payloadJson)
    {
        return new OutboxMessage
        {
            Id = integrationEvent.EventId,
            ModuleCode = moduleCode,
            EventType = integrationEvent.GetType().FullName ?? integrationEvent.GetType().Name,
            PayloadJson = payloadJson,
            OccurredAtUtc = integrationEvent.OccurredAtUtc
        };
    }
}
