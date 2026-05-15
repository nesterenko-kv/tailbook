namespace Tailbook.BuildingBlocks.Abstractions;

public interface IOutboxPublisher
{
    Task PublishAsync(string moduleCode, string eventType, object payload, CancellationToken cancellationToken);
}
