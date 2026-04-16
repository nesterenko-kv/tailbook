namespace Tailbook.BuildingBlocks.Abstractions;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredAtUtc { get; }
}
