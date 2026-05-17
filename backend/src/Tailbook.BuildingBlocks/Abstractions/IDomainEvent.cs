namespace Tailbook.BuildingBlocks.Abstractions;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
    string EventType { get; }
    string ModuleCode { get; }
}
