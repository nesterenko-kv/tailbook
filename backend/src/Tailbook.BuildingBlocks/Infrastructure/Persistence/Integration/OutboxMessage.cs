namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
