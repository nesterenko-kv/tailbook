namespace Tailbook.BuildingBlocks.Abstractions;

public interface IMessageBroker
{
    Task PublishAsync(string exchange, string routingKey, object payload, CancellationToken cancellationToken = default);

    Task PublishAsync(string exchange, string routingKey, object payload, string? messageId, CancellationToken cancellationToken = default);
}
