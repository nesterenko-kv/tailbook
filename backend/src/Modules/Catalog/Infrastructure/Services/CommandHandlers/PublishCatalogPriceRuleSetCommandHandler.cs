using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Tailbook.BuildingBlocks.Infrastructure;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Catalog.Infrastructure.Services.CommandHandlers;

public sealed class PublishCatalogPriceRuleSetCommandHandler(AppDbContext dbContext, TimeProvider timeProvider, IDistributedCache cache)
    : ICommandHandler<PublishCatalogPriceRuleSetCommand, ErrorOr<PriceRuleSetView>>
{
    public async Task<ErrorOr<PriceRuleSetView>> ExecuteAsync(PublishCatalogPriceRuleSetCommand command, CancellationToken cancellationToken)
    {
        var ruleSet = await LoadPriceRuleSetAggregateAsync(command.RuleSetId, cancellationToken);
        if (ruleSet is null)
        {
            return Error.NotFound("Catalog.PriceRuleSetNotFound", "Price rule set does not exist.");
        }

        var publish = ruleSet.Publish(timeProvider.GetUtcNow());
        if (publish.IsError)
        {
            return publish.Errors;
        }

        var publishedSets = await dbContext.Set<PriceRuleSet>()
            .Where(x => x.Id != command.RuleSetId && x.Status == RuleSetStatusCodes.Published)
            .ToListAsync(cancellationToken);

        foreach (var publishedSet in publishedSets)
        {
            publishedSet.Archive();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(CacheKeys.PriceRuleSetActive(), cancellationToken);

        return await MapRuleSetAsync(ruleSet, cancellationToken);
    }

    private async Task<PriceRuleSet?> LoadPriceRuleSetAggregateAsync(Guid ruleSetId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<PriceRuleSet>()
            .Include(x => x.Rules)
            .ThenInclude(x => x.Condition)
            .SingleOrDefaultAsync(x => x.Id == ruleSetId, cancellationToken);
    }

    private async Task<PriceRuleSetView> MapRuleSetAsync(PriceRuleSet ruleSet, CancellationToken cancellationToken)
    {
        var offerIds = ruleSet.Rules.Select(x => x.OfferId).Distinct().ToArray();
        var offers = await dbContext.Set<CommercialOffer>()
            .Where(x => offerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return new PriceRuleSetView(
            ruleSet.Id,
            ruleSet.VersionNo,
            ruleSet.Status,
            ruleSet.ValidFrom,
            ruleSet.ValidTo,
            ruleSet.CreatedAt,
            ruleSet.PublishedAt,
            ruleSet.Rules.OrderBy(x => x.Priority)
                .Select(rule => CatalogRuleViewMapper.ToView(rule, rule.Condition, offers[rule.OfferId]))
                .ToArray());
    }

}
