using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Catalog.IntegrationEvents;

public sealed record PriceRuleSetCreatedIntegrationEvent(
    Guid RuleSetId,
    int VersionNo,
    string Status,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo) : IIntegrationEventDto
{
    public int EventVersion => CatalogIntegrationEventVersions.PriceRuleSetCreated;
}
