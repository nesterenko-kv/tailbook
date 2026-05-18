using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Catalog.Contracts.IntegrationEvents;

namespace Tailbook.Modules.Catalog.Domain.Events;

public sealed record PriceRuleSetCreatedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid RuleSetId,
    int VersionNo,
    string Status,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo) : IDomainEvent
{
    public string EventType => "PriceRuleSetCreated";
    public string ModuleCode => "catalog";

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new PriceRuleSetCreatedIntegrationEvent(
            RuleSetId,
            VersionNo,
            Status,
            ValidFrom,
            ValidTo);
    }
}
