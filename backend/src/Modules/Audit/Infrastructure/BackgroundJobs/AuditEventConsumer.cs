using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Tailbook.Modules.Audit.Infrastructure.WriteBuffering;

namespace Tailbook.Modules.Audit.Infrastructure.BackgroundJobs;

public sealed class AuditEventConsumer(
    RabbitMqConnectionFactory connectionFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<AuditEventConsumer> logger
)
    : IntegrationEventConsumerBase(connectionFactory, rabbitMqOptions, scopeFactory, logger)
{
    protected override string QueueName => "audit";
    protected override string[] RoutingKeys => ["#"];

    protected override async Task ProcessEventAsync(
        string eventType,
        string innerPayload,
        Guid messageId,
        string routingKey,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = ScopeFactory.CreateScope();
        IAuditWriteQueue queue = scope.ServiceProvider.GetRequiredService<IAuditWriteQueue>();

        string moduleCode = routingKey.Split('.').FirstOrDefault() ?? "unknown";
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

        logger.AuditEventProcessed(messageId, eventType, routingKey);
    }
}
