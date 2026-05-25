using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Catalog.IntegrationEvents;

public sealed record PriceRuleAddedIntegrationEvent(
    Guid RuleSetId,
    Guid RuleId,
    Guid OfferId,
    int Priority,
    decimal FixedAmount,
    string Currency) : IIntegrationEventDto
{
    public int EventVersion => CatalogIntegrationEventVersions.PriceRuleAdded;
}
