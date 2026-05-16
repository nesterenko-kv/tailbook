namespace Tailbook.BuildingBlocks.Abstractions;

public interface IOutboxPublisher
{
    ValueTask PublishAsync(string moduleCode, string eventType, object payload, CancellationToken cancellationToken);
}
