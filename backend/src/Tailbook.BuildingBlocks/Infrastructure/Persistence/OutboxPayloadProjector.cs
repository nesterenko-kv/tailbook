using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence;

internal static class OutboxPayloadProjector
{
    public static IIntegrationEventDto Project(IDomainEvent domainEvent)
    {
        var integrationEvent = domainEvent.ToIntegrationEvent();
        IntegrationEventVersionPolicy.EnsureValid(integrationEvent.EventVersion, domainEvent.EventType);
        return integrationEvent;
    }
}
