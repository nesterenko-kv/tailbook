using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Catalog.Infrastructure.Services;

namespace Tailbook.Modules.Catalog.Infrastructure.Services.CommandHandlers;

public sealed class PublishCatalogDurationRuleSetCommandHandler(AppDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<PublishCatalogDurationRuleSetCommand, ErrorOr<DurationRuleSetView>>
{
    public async Task<ErrorOr<DurationRuleSetView>> ExecuteAsync(PublishCatalogDurationRuleSetCommand command, CancellationToken cancellationToken)
    {
        var ruleSet = await LoadDurationRuleSetAggregateAsync(command.RuleSetId, cancellationToken);
        if (ruleSet is null)
        {
            return Error.NotFound("Catalog.DurationRuleSetNotFound", "Duration rule set does not exist.");
        }

        var publish = ruleSet.Publish(timeProvider.GetUtcNow());
        if (publish.IsError)
        {
            return publish.Errors;
        }

        var publishedSets = await dbContext.Set<DurationRuleSet>()
            .Where(x => x.Id != command.RuleSetId && x.Status == RuleSetStatusCodes.Published)
            .ToListAsync(cancellationToken);

        foreach (var publishedSet in publishedSets)
        {
            publishedSet.Archive();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapRuleSetAsync(ruleSet, cancellationToken);
    }

    private async Task<DurationRuleSet?> LoadDurationRuleSetAggregateAsync(Guid ruleSetId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<DurationRuleSet>()
            .Include(x => x.Rules)
            .ThenInclude(x => x.Condition)
            .SingleOrDefaultAsync(x => x.Id == ruleSetId, cancellationToken);
    }

    private async Task<DurationRuleSetView> MapRuleSetAsync(DurationRuleSet ruleSet, CancellationToken cancellationToken)
    {
        var offerIds = ruleSet.Rules.Select(x => x.OfferId).Distinct().ToArray();
        var offers = await dbContext.Set<CommercialOffer>()
            .Where(x => offerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return new DurationRuleSetView(
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
