using ErrorOr;

namespace Tailbook.Modules.Catalog.Application.Catalog.Queries;

public interface ICatalogPricingQueries
{
    Task<IReadOnlyCollection<PriceRuleSetView>> ListPriceRuleSetsAsync(CancellationToken cancellationToken);
    Task<PriceRuleSetView> CreatePriceRuleSetAsync(DateTime? validFromUtc, DateTime? validToUtc, CancellationToken cancellationToken);
    Task<ErrorOr<PriceRuleView>> CreatePriceRuleAsync(CreatePriceRuleCommand command, CancellationToken cancellationToken);
    Task<ErrorOr<PriceRuleSetView>> PublishPriceRuleSetAsync(Guid ruleSetId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DurationRuleSetView>> ListDurationRuleSetsAsync(CancellationToken cancellationToken);
    Task<DurationRuleSetView> CreateDurationRuleSetAsync(DateTime? validFromUtc, DateTime? validToUtc, CancellationToken cancellationToken);
    Task<ErrorOr<DurationRuleView>> CreateDurationRuleAsync(CreateDurationRuleCommand command, CancellationToken cancellationToken);
    Task<ErrorOr<DurationRuleSetView>> PublishDurationRuleSetAsync(Guid ruleSetId, CancellationToken cancellationToken);
}
