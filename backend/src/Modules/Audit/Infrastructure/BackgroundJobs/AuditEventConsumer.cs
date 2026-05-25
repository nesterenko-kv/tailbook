using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Tailbook.Modules.Audit.Infrastructure.WriteBuffering;

namespace Tailbook.Modules.Audit.Infrastructure.BackgroundJobs;

public sealed class AuditEventConsumer : IntegrationEventConsumerBase
{
    private readonly ILogger<AuditEventConsumer> _logger;

    protected override string QueueName => "audit";
    protected override string[] RoutingKeys => ["#"];

    public AuditEventConsumer(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<AuditEventConsumer> logger
    ) : base(connectionFactory, rabbitMqOptions, scopeFactory, logger)
    {
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(
        string eventType,
        string innerPayload,
        Guid messageId,
        string routingKey,
        CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();
        var queue = scope.ServiceProvider.GetRequiredService<IAuditWriteQueue>();

        var moduleCode = routingKey.Split('.').FirstOrDefault() ?? "unknown";
        var item = new AuditTrailWriteItem(
            messageId,
            null,
            moduleCode,
            eventType,
            routingKey,
            "consumed",
            DateTimeOffset.UtcNow,
            null,
            innerPayload
        );

        await queue.EnqueueAsync(item, cancellationToken);

        _logger.AuditEventProcessed(messageId, eventType, routingKey);
    }
}
