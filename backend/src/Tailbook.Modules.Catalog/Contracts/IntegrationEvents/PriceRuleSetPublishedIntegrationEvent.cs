using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Catalog.Contracts.IntegrationEvents;

public sealed record PriceRuleSetPublishedIntegrationEvent(
    Guid RuleSetId,
    int VersionNo,
    string Status,
    DateTimeOffset PublishedAt) : IIntegrationEventDto
{
    public int EventVersion => CatalogIntegrationEventVersions.PriceRuleSetPublished;
}
