using Microsoft.Extensions.Logging;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.BuildingBlocks.Infrastructure.Messaging;

public sealed class NoOpMessageBroker : IMessageBroker
{
    private readonly ILogger<NoOpMessageBroker> _logger;

    public NoOpMessageBroker(ILogger<NoOpMessageBroker> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(string exchange, string routingKey, object payload, CancellationToken cancellationToken = default)
    {
        return PublishAsync(exchange, routingKey, payload, messageId: null, cancellationToken);
    }

    public Task PublishAsync(string exchange, string routingKey, object payload, string? messageId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "NoOp broker: would publish to exchange {Exchange} with routing key {RoutingKey}. " +
            "Configure RabbitMq:Enabled to true to enable actual message broker publishing.",
            exchange, routingKey);

        return Task.CompletedTask;
    }
}
