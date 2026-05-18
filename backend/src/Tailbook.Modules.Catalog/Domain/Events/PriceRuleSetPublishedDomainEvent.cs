using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Catalog.Contracts.IntegrationEvents;

namespace Tailbook.Modules.Catalog.Domain.Events;

public sealed record PriceRuleSetPublishedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid RuleSetId,
    int VersionNo,
    string Status,
    DateTimeOffset PublishedAt) : IDomainEvent
{
    public string EventType => "PriceRuleSetPublished";
    public string ModuleCode => "catalog";

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new PriceRuleSetPublishedIntegrationEvent(
            RuleSetId,
            VersionNo,
            Status,
            PublishedAt);
    }
}
