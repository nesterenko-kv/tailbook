using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Catalog.Infrastructure.Services;

public sealed class CatalogPricingReadService(AppDbContext dbContext) : ICatalogPricingReadService
{
    public async Task<IReadOnlyCollection<PriceRuleSetView>> ListPriceRuleSetsAsync(CancellationToken cancellationToken)
    {
        var ruleSets = await dbContext.Set<PriceRuleSet>()
            .OrderByDescending(x => x.VersionNo)
            .ToListAsync(cancellationToken);

        var ruleSetIds = ruleSets.Select(x => x.Id).ToArray();
        var rules = await dbContext.Set<PriceRule>()
            .Where(x => ruleSetIds.Contains(x.RuleSetId))
            .OrderBy(x => x.Priority)
            .ToListAsync(cancellationToken);
        var conditions = await dbContext.Set<PriceRuleCondition>()
            .Where(x => rules.Select(r => r.Id).Contains(x.PriceRuleId))
            .ToDictionaryAsync(x => x.PriceRuleId, cancellationToken);
        var offers = await dbContext.Set<CommercialOffer>()
            .Where(x => rules.Select(r => r.OfferId).Distinct().Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return ruleSets.Select(ruleSet => new PriceRuleSetView(
            ruleSet.Id,
            ruleSet.VersionNo,
            ruleSet.Status,
            ruleSet.ValidFrom,
            ruleSet.ValidTo,
            ruleSet.CreatedAt,
            ruleSet.PublishedAt,
            rules.Where(x => x.RuleSetId == ruleSet.Id)
                .Select(rule => CatalogRuleViewMapper.ToView(rule, conditions.GetValueOrDefault(rule.Id), offers[rule.OfferId]))
                .ToArray()))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<DurationRuleSetView>> ListDurationRuleSetsAsync(CancellationToken cancellationToken)
    {
        var ruleSets = await dbContext.Set<DurationRuleSet>()
            .OrderByDescending(x => x.VersionNo)
            .ToListAsync(cancellationToken);

        var ruleSetIds = ruleSets.Select(x => x.Id).ToArray();
        var rules = await dbContext.Set<DurationRule>()
            .Where(x => ruleSetIds.Contains(x.RuleSetId))
            .OrderBy(x => x.Priority)
            .ToListAsync(cancellationToken);
        var conditions = await dbContext.Set<DurationRuleCondition>()
            .Where(x => rules.Select(r => r.Id).Contains(x.DurationRuleId))
            .ToDictionaryAsync(x => x.DurationRuleId, cancellationToken);
        var offers = await dbContext.Set<CommercialOffer>()
            .Where(x => rules.Select(r => r.OfferId).Distinct().Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return ruleSets.Select(ruleSet => new DurationRuleSetView(
            ruleSet.Id,
            ruleSet.VersionNo,
            ruleSet.Status,
            ruleSet.ValidFrom,
            ruleSet.ValidTo,
            ruleSet.CreatedAt,
            ruleSet.PublishedAt,
            rules.Where(x => x.RuleSetId == ruleSet.Id)
                .Select(rule => CatalogRuleViewMapper.ToView(rule, conditions.GetValueOrDefault(rule.Id), offers[rule.OfferId]))
                .ToArray()))
            .ToArray();
    }

}
