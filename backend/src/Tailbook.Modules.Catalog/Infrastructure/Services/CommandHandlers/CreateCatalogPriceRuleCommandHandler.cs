using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Catalog.Infrastructure.Services;

namespace Tailbook.Modules.Catalog.Infrastructure.Services.CommandHandlers;

public sealed class CreateCatalogPriceRuleCommandHandler(
    AppDbContext dbContext,
    IPetTaxonomyValidationService petTaxonomyValidationService,
    TimeProvider timeProvider)
    : ICommandHandler<CreateCatalogPriceRuleCommand, ErrorOr<PriceRuleView>>
{
    public async Task<ErrorOr<PriceRuleView>> ExecuteAsync(CreateCatalogPriceRuleCommand command, CancellationToken cancellationToken)
    {
        var contextData = command.Rule;
        var ruleSet = await LoadPriceRuleSetAggregateAsync(contextData.RuleSetId, cancellationToken);
        if (ruleSet is null)
        {
            return Error.NotFound("Catalog.PriceRuleSetNotFound", "Price rule set does not exist.");
        }

        var canAdd = ruleSet.EnsureCanAddRule();
        if (canAdd.IsError)
        {
            return canAdd.Errors;
        }

        var offer = await dbContext.Set<CommercialOffer>().SingleOrDefaultAsync(x => x.Id == contextData.OfferId, cancellationToken);
        if (offer is null)
        {
            return Error.NotFound("Catalog.OfferNotFound", "Offer does not exist.");
        }

        var taxonomy = await CatalogRuleConditionValidator.ValidateAsync(contextData.Condition, petTaxonomyValidationService, cancellationToken);
        if (taxonomy.IsError)
        {
            return taxonomy.Errors;
        }

        var rule = ruleSet.AddRule(
            contextData.OfferId,
            contextData.Priority,
            contextData.FixedAmount,
            contextData.Currency,
            contextData.Condition.AnimalTypeId,
            contextData.Condition.BreedId,
            contextData.Condition.BreedGroupId,
            contextData.Condition.CoatTypeId,
            contextData.Condition.SizeCategoryId,
            timeProvider.GetUtcNow());
        if (rule.IsError)
        {
            return rule.Errors;
        }

        dbContext.Set<PriceRule>().Add(rule.Value);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CatalogRuleViewMapper.ToView(rule.Value, rule.Value.Condition, offer);
    }

    private async Task<PriceRuleSet?> LoadPriceRuleSetAggregateAsync(Guid ruleSetId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<PriceRuleSet>()
            .Include(x => x.Rules)
            .ThenInclude(x => x.Condition)
            .SingleOrDefaultAsync(x => x.Id == ruleSetId, cancellationToken);
    }
}
