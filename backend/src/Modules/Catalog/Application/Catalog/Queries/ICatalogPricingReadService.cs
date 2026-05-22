namespace Tailbook.Modules.Catalog.Application.Catalog.Queries;

public interface ICatalogPricingReadService
{
    Task<IReadOnlyCollection<PriceRuleSetView>> ListPriceRuleSetsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DurationRuleSetView>> ListDurationRuleSetsAsync(CancellationToken cancellationToken);
}
