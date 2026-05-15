using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Catalog.Infrastructure.Services;

namespace Tailbook.Modules.Catalog.Infrastructure.Services.CommandHandlers;

public sealed class CreateCatalogDurationRuleCommandHandler(
    AppDbContext dbContext,
    IPetTaxonomyValidationService petTaxonomyValidationService,
    TimeProvider timeProvider)
    : ICommandHandler<CreateCatalogDurationRuleCommand, ErrorOr<DurationRuleView>>
{
    public async Task<ErrorOr<DurationRuleView>> ExecuteAsync(CreateCatalogDurationRuleCommand command, CancellationToken cancellationToken)
    {
        var ruleSet = await LoadDurationRuleSetAggregateAsync(command.RuleSetId, cancellationToken);
        if (ruleSet is null)
        {
            return Error.NotFound("Catalog.DurationRuleSetNotFound", "Duration rule set does not exist.");
        }

        var canAdd = ruleSet.EnsureCanAddRule();
        if (canAdd.IsError)
        {
            return canAdd.Errors;
        }

        var offer = await dbContext.Set<CommercialOffer>().SingleOrDefaultAsync(x => x.Id == command.OfferId, cancellationToken);
        if (offer is null)
        {
            return Error.NotFound("Catalog.OfferNotFound", "Offer does not exist.");
        }

        var taxonomy = await CatalogRuleConditionValidator.ValidateAsync(command.Condition, petTaxonomyValidationService, cancellationToken);
        if (taxonomy.IsError)
        {
            return taxonomy.Errors;
        }

        var rule = ruleSet.AddRule(
            command.OfferId,
            command.Priority,
            command.BaseMinutes,
            command.BufferBeforeMinutes,
            command.BufferAfterMinutes,
            command.Condition.AnimalTypeId,
            command.Condition.BreedId,
            command.Condition.BreedGroupId,
            command.Condition.CoatTypeId,
            command.Condition.SizeCategoryId,
            timeProvider.GetUtcNow());
        if (rule.IsError)
        {
            return rule.Errors;
        }

        dbContext.Set<DurationRule>().Add(rule.Value);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CatalogRuleViewMapper.ToView(rule.Value, rule.Value.Condition, offer);
    }

    private async Task<DurationRuleSet?> LoadDurationRuleSetAggregateAsync(Guid ruleSetId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<DurationRuleSet>()
            .Include(x => x.Rules)
            .ThenInclude(x => x.Condition)
            .SingleOrDefaultAsync(x => x.Id == ruleSetId, cancellationToken);
    }
}
