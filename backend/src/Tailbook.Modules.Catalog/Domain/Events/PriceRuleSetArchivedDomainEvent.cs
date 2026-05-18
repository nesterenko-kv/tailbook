using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Catalog.Contracts.IntegrationEvents;

namespace Tailbook.Modules.Catalog.Domain.Events;

public sealed record PriceRuleSetArchivedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid RuleSetId,
    int VersionNo,
    string Status) : IDomainEvent
{
    public string EventType => "PriceRuleSetArchived";
    public string ModuleCode => "catalog";

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new PriceRuleSetArchivedIntegrationEvent(
            RuleSetId,
            VersionNo,
            Status);
    }
}
