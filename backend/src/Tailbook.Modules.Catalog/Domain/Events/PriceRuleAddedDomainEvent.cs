using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Catalog.Contracts.IntegrationEvents;

namespace Tailbook.Modules.Catalog.Domain.Events;

public sealed record PriceRuleAddedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid RuleSetId,
    Guid RuleId,
    Guid OfferId,
    int Priority,
    decimal FixedAmount,
    string Currency) : IDomainEvent
{
    public string EventType => "PriceRuleAdded";
    public string ModuleCode => "catalog";

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new PriceRuleAddedIntegrationEvent(
            RuleSetId,
            RuleId,
            OfferId,
            Priority,
            FixedAmount,
            Currency);
    }
}
