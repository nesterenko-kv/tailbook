using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Catalog.Contracts.IntegrationEvents;

public static class CatalogIntegrationEventVersions
{
    public const int PriceRuleSetCreated = IntegrationEventVersionPolicy.InitialVersion;
    public const int PriceRuleSetPublished = IntegrationEventVersionPolicy.InitialVersion;
    public const int PriceRuleSetArchived = IntegrationEventVersionPolicy.InitialVersion;
    public const int PriceRuleAdded = IntegrationEventVersionPolicy.InitialVersion;
}
