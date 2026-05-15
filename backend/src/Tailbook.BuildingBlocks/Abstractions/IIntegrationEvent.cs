namespace Tailbook.BuildingBlocks.Abstractions;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}
