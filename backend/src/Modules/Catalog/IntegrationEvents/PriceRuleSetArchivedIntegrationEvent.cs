using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Catalog.IntegrationEvents;

public sealed record PriceRuleSetArchivedIntegrationEvent(
    Guid RuleSetId,
    int VersionNo,
    string Status) : IIntegrationEventDto
{
    public int EventVersion => CatalogIntegrationEventVersions.PriceRuleSetArchived;
}
