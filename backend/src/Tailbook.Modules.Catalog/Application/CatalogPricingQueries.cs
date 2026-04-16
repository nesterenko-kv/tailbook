using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Catalog.Contracts;
using Tailbook.Modules.Catalog.Domain;

namespace Tailbook.Modules.Catalog.Application;

public sealed class CatalogPricingQueries(AppDbContext dbContext, IPetTaxonomyValidationService petTaxonomyValidationService)
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
            ruleSet.ValidFromUtc,
            ruleSet.ValidToUtc,
            ruleSet.CreatedAtUtc,
            ruleSet.PublishedAtUtc,
            rules.Where(x => x.RuleSetId == ruleSet.Id)
                .Select(rule => MapPriceRule(rule, conditions.GetValueOrDefault(rule.Id), offers[rule.OfferId]))
                .ToArray()))
            .ToArray();
    }

    public async Task<PriceRuleSetView> CreatePriceRuleSetAsync(DateTime? validFromUtc, DateTime? validToUtc, CancellationToken cancellationToken)
    {
        var nextVersionNo = (await dbContext.Set<PriceRuleSet>().MaxAsync(x => (int?)x.VersionNo, cancellationToken) ?? 0) + 1;
        var utcNow = DateTime.UtcNow;
        var entity = new PriceRuleSet
        {
            Id = Guid.NewGuid(),
            VersionNo = nextVersionNo,
            Status = RuleSetStatusCodes.Draft,
            ValidFromUtc = validFromUtc ?? utcNow,
            ValidToUtc = validToUtc,
            CreatedAtUtc = utcNow
        };

        dbContext.Set<PriceRuleSet>().Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new PriceRuleSetView(entity.Id, entity.VersionNo, entity.Status, entity.ValidFromUtc, entity.ValidToUtc, entity.CreatedAtUtc, entity.PublishedAtUtc, []);
    }

    public async Task<PriceRuleView?> CreatePriceRuleAsync(CreatePriceRuleCommand command, CancellationToken cancellationToken)
    {
        var ruleSet = await dbContext.Set<PriceRuleSet>().SingleOrDefaultAsync(x => x.Id == command.RuleSetId, cancellationToken);
        if (ruleSet is null)
        {
            return null;
        }

        if (!string.Equals(ruleSet.Status, RuleSetStatusCodes.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Price rules can only be added to draft rule sets.");
        }

        var offer = await dbContext.Set<CommercialOffer>().SingleOrDefaultAsync(x => x.Id == command.OfferId, cancellationToken)
            ?? throw new InvalidOperationException("Offer does not exist.");

        await ValidateTaxonomyAsync(command.Condition, cancellationToken);

        var duplicate = await dbContext.Set<PriceRule>()
            .Where(x => x.RuleSetId == command.RuleSetId && x.OfferId == command.OfferId)
            .Join(dbContext.Set<PriceRuleCondition>(), x => x.Id, y => y.PriceRuleId, (x, y) => new { Rule = x, Condition = y })
            .AnyAsync(x => x.Condition.AnimalTypeId == command.Condition.AnimalTypeId
                           && x.Condition.BreedId == command.Condition.BreedId
                           && x.Condition.BreedGroupId == command.Condition.BreedGroupId
                           && x.Condition.CoatTypeId == command.Condition.CoatTypeId
                           && x.Condition.SizeCategoryId == command.Condition.SizeCategoryId,
                cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("An equivalent price rule already exists in this rule set for the same offer and condition combination.");
        }

        var utcNow = DateTime.UtcNow;
        var rule = new PriceRule
        {
            Id = Guid.NewGuid(),
            RuleSetId = command.RuleSetId,
            OfferId = command.OfferId,
            Priority = command.Priority,
            SpecificityScore = CalculateSpecificity(command.Condition),
            ActionType = PriceRuleActionTypes.FixedAmount,
            FixedAmount = command.FixedAmount,
            Currency = NormalizeCurrency(command.Currency),
            CreatedAtUtc = utcNow
        };

        var condition = new PriceRuleCondition
        {
            Id = Guid.NewGuid(),
            PriceRuleId = rule.Id,
            AnimalTypeId = command.Condition.AnimalTypeId,
            BreedId = command.Condition.BreedId,
            BreedGroupId = command.Condition.BreedGroupId,
            CoatTypeId = command.Condition.CoatTypeId,
            SizeCategoryId = command.Condition.SizeCategoryId
        };

        dbContext.Set<PriceRule>().Add(rule);
        dbContext.Set<PriceRuleCondition>().Add(condition);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapPriceRule(rule, condition, offer);
    }

    public async Task<PriceRuleSetView?> PublishPriceRuleSetAsync(Guid ruleSetId, CancellationToken cancellationToken)
    {
        var ruleSet = await dbContext.Set<PriceRuleSet>().SingleOrDefaultAsync(x => x.Id == ruleSetId, cancellationToken);
        if (ruleSet is null)
        {
            return null;
        }

        if (!string.Equals(ruleSet.Status, RuleSetStatusCodes.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only draft price rule sets can be published.");
        }

        var hasRules = await dbContext.Set<PriceRule>().AnyAsync(x => x.RuleSetId == ruleSetId, cancellationToken);
        if (!hasRules)
        {
            throw new InvalidOperationException("A price rule set must contain at least one rule before publication.");
        }

        var publishedSets = await dbContext.Set<PriceRuleSet>()
            .Where(x => x.Id != ruleSetId && x.Status == RuleSetStatusCodes.Published)
            .ToListAsync(cancellationToken);

        foreach (var publishedSet in publishedSets)
        {
            publishedSet.Status = RuleSetStatusCodes.Archived;
        }

        ruleSet.Status = RuleSetStatusCodes.Published;
        ruleSet.PublishedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetPriceRuleSetAsync(ruleSetId, cancellationToken);
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
            ruleSet.ValidFromUtc,
            ruleSet.ValidToUtc,
            ruleSet.CreatedAtUtc,
            ruleSet.PublishedAtUtc,
            rules.Where(x => x.RuleSetId == ruleSet.Id)
                .Select(rule => MapDurationRule(rule, conditions.GetValueOrDefault(rule.Id), offers[rule.OfferId]))
                .ToArray()))
            .ToArray();
    }

    public async Task<DurationRuleSetView> CreateDurationRuleSetAsync(DateTime? validFromUtc, DateTime? validToUtc, CancellationToken cancellationToken)
    {
        var nextVersionNo = (await dbContext.Set<DurationRuleSet>().MaxAsync(x => (int?)x.VersionNo, cancellationToken) ?? 0) + 1;
        var utcNow = DateTime.UtcNow;
        var entity = new DurationRuleSet
        {
            Id = Guid.NewGuid(),
            VersionNo = nextVersionNo,
            Status = RuleSetStatusCodes.Draft,
            ValidFromUtc = validFromUtc ?? utcNow,
            ValidToUtc = validToUtc,
            CreatedAtUtc = utcNow
        };

        dbContext.Set<DurationRuleSet>().Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new DurationRuleSetView(entity.Id, entity.VersionNo, entity.Status, entity.ValidFromUtc, entity.ValidToUtc, entity.CreatedAtUtc, entity.PublishedAtUtc, []);
    }

    public async Task<DurationRuleView?> CreateDurationRuleAsync(CreateDurationRuleCommand command, CancellationToken cancellationToken)
    {
        var ruleSet = await dbContext.Set<DurationRuleSet>().SingleOrDefaultAsync(x => x.Id == command.RuleSetId, cancellationToken);
        if (ruleSet is null)
        {
            return null;
        }

        if (!string.Equals(ruleSet.Status, RuleSetStatusCodes.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Duration rules can only be added to draft rule sets.");
        }

        var offer = await dbContext.Set<CommercialOffer>().SingleOrDefaultAsync(x => x.Id == command.OfferId, cancellationToken)
            ?? throw new InvalidOperationException("Offer does not exist.");

        await ValidateTaxonomyAsync(command.Condition, cancellationToken);

        var duplicate = await dbContext.Set<DurationRule>()
            .Where(x => x.RuleSetId == command.RuleSetId && x.OfferId == command.OfferId)
            .Join(dbContext.Set<DurationRuleCondition>(), x => x.Id, y => y.DurationRuleId, (x, y) => new { Rule = x, Condition = y })
            .AnyAsync(x => x.Condition.AnimalTypeId == command.Condition.AnimalTypeId
                           && x.Condition.BreedId == command.Condition.BreedId
                           && x.Condition.BreedGroupId == command.Condition.BreedGroupId
                           && x.Condition.CoatTypeId == command.Condition.CoatTypeId
                           && x.Condition.SizeCategoryId == command.Condition.SizeCategoryId,
                cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("An equivalent duration rule already exists in this rule set for the same offer and condition combination.");
        }

        var utcNow = DateTime.UtcNow;
        var rule = new DurationRule
        {
            Id = Guid.NewGuid(),
            RuleSetId = command.RuleSetId,
            OfferId = command.OfferId,
            Priority = command.Priority,
            SpecificityScore = CalculateSpecificity(command.Condition),
            BaseMinutes = command.BaseMinutes,
            BufferBeforeMinutes = command.BufferBeforeMinutes,
            BufferAfterMinutes = command.BufferAfterMinutes,
            CreatedAtUtc = utcNow
        };

        var condition = new DurationRuleCondition
        {
            Id = Guid.NewGuid(),
            DurationRuleId = rule.Id,
            AnimalTypeId = command.Condition.AnimalTypeId,
            BreedId = command.Condition.BreedId,
            BreedGroupId = command.Condition.BreedGroupId,
            CoatTypeId = command.Condition.CoatTypeId,
            SizeCategoryId = command.Condition.SizeCategoryId
        };

        dbContext.Set<DurationRule>().Add(rule);
        dbContext.Set<DurationRuleCondition>().Add(condition);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapDurationRule(rule, condition, offer);
    }

    public async Task<DurationRuleSetView?> PublishDurationRuleSetAsync(Guid ruleSetId, CancellationToken cancellationToken)
    {
        var ruleSet = await dbContext.Set<DurationRuleSet>().SingleOrDefaultAsync(x => x.Id == ruleSetId, cancellationToken);
        if (ruleSet is null)
        {
            return null;
        }

        if (!string.Equals(ruleSet.Status, RuleSetStatusCodes.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only draft duration rule sets can be published.");
        }

        var hasRules = await dbContext.Set<DurationRule>().AnyAsync(x => x.RuleSetId == ruleSetId, cancellationToken);
        if (!hasRules)
        {
            throw new InvalidOperationException("A duration rule set must contain at least one rule before publication.");
        }

        var publishedSets = await dbContext.Set<DurationRuleSet>()
            .Where(x => x.Id != ruleSetId && x.Status == RuleSetStatusCodes.Published)
            .ToListAsync(cancellationToken);

        foreach (var publishedSet in publishedSets)
        {
            publishedSet.Status = RuleSetStatusCodes.Archived;
        }

        ruleSet.Status = RuleSetStatusCodes.Published;
        ruleSet.PublishedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetDurationRuleSetAsync(ruleSetId, cancellationToken);
    }

    private async Task<PriceRuleSetView> GetPriceRuleSetAsync(Guid ruleSetId, CancellationToken cancellationToken)
    {
        var ruleSets = await ListPriceRuleSetsAsync(cancellationToken);
        return ruleSets.Single(x => x.Id == ruleSetId);
    }

    private async Task<DurationRuleSetView> GetDurationRuleSetAsync(Guid ruleSetId, CancellationToken cancellationToken)
    {
        var ruleSets = await ListDurationRuleSetsAsync(cancellationToken);
        return ruleSets.Single(x => x.Id == ruleSetId);
    }

    private async Task ValidateTaxonomyAsync(RuleConditionInput condition, CancellationToken cancellationToken)
    {
        if (condition.AnimalTypeId is not null && !await petTaxonomyValidationService.AnimalTypeExistsAsync(condition.AnimalTypeId.Value, cancellationToken))
        {
            throw new InvalidOperationException("Animal type does not exist.");
        }

        if (condition.BreedId is not null && !await petTaxonomyValidationService.BreedExistsAsync(condition.BreedId.Value, cancellationToken))
        {
            throw new InvalidOperationException("Breed does not exist.");
        }

        if (condition.BreedGroupId is not null && !await petTaxonomyValidationService.BreedGroupExistsAsync(condition.BreedGroupId.Value, cancellationToken))
        {
            throw new InvalidOperationException("Breed group does not exist.");
        }

        if (condition.CoatTypeId is not null && !await petTaxonomyValidationService.CoatTypeExistsAsync(condition.CoatTypeId.Value, cancellationToken))
        {
            throw new InvalidOperationException("Coat type does not exist.");
        }

        if (condition.SizeCategoryId is not null && !await petTaxonomyValidationService.SizeCategoryExistsAsync(condition.SizeCategoryId.Value, cancellationToken))
        {
            throw new InvalidOperationException("Size category does not exist.");
        }
    }

    private static int CalculateSpecificity(RuleConditionInput condition)
    {
        var values = new Guid?[]
        {
            condition.AnimalTypeId,
            condition.BreedId,
            condition.BreedGroupId,
            condition.CoatTypeId,
            condition.SizeCategoryId
        };

        return values.Count(x => x.HasValue);
    }

    private static string NormalizeCurrency(string currency)
    {
        var normalized = currency.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Currency is required.");
        }

        return normalized;
    }

    private static PriceRuleView MapPriceRule(PriceRule rule, PriceRuleCondition? condition, CommercialOffer offer)
    {
        return new PriceRuleView(
            rule.Id,
            rule.RuleSetId,
            rule.OfferId,
            offer.Code,
            offer.DisplayName,
            rule.Priority,
            rule.SpecificityScore,
            rule.ActionType,
            rule.FixedAmount,
            rule.Currency,
            new RuleConditionView(
                condition?.AnimalTypeId,
                condition?.BreedId,
                condition?.BreedGroupId,
                condition?.CoatTypeId,
                condition?.SizeCategoryId),
            rule.CreatedAtUtc);
    }

    private static DurationRuleView MapDurationRule(DurationRule rule, DurationRuleCondition? condition, CommercialOffer offer)
    {
        return new DurationRuleView(
            rule.Id,
            rule.RuleSetId,
            rule.OfferId,
            offer.Code,
            offer.DisplayName,
            rule.Priority,
            rule.SpecificityScore,
            rule.BaseMinutes,
            rule.BufferBeforeMinutes,
            rule.BufferAfterMinutes,
            new RuleConditionView(
                condition?.AnimalTypeId,
                condition?.BreedId,
                condition?.BreedGroupId,
                condition?.CoatTypeId,
                condition?.SizeCategoryId),
            rule.CreatedAtUtc);
    }
}

public sealed record CreatePriceRuleCommand(Guid RuleSetId, Guid OfferId, int Priority, decimal FixedAmount, string Currency, RuleConditionInput Condition);
public sealed record CreateDurationRuleCommand(Guid RuleSetId, Guid OfferId, int Priority, int BaseMinutes, int BufferBeforeMinutes, int BufferAfterMinutes, RuleConditionInput Condition);
public sealed record RuleConditionInput(Guid? AnimalTypeId, Guid? BreedId, Guid? BreedGroupId, Guid? CoatTypeId, Guid? SizeCategoryId);

public sealed record RuleConditionView(Guid? AnimalTypeId, Guid? BreedId, Guid? BreedGroupId, Guid? CoatTypeId, Guid? SizeCategoryId);
public sealed record PriceRuleSetView(Guid Id, int VersionNo, string Status, DateTime ValidFromUtc, DateTime? ValidToUtc, DateTime CreatedAtUtc, DateTime? PublishedAtUtc, IReadOnlyCollection<PriceRuleView> Rules);
public sealed record PriceRuleView(Guid Id, Guid RuleSetId, Guid OfferId, string OfferCode, string OfferDisplayName, int Priority, int SpecificityScore, string ActionType, decimal FixedAmount, string Currency, RuleConditionView Condition, DateTime CreatedAtUtc);
public sealed record DurationRuleSetView(Guid Id, int VersionNo, string Status, DateTime ValidFromUtc, DateTime? ValidToUtc, DateTime CreatedAtUtc, DateTime? PublishedAtUtc, IReadOnlyCollection<DurationRuleView> Rules);
public sealed record DurationRuleView(Guid Id, Guid RuleSetId, Guid OfferId, string OfferCode, string OfferDisplayName, int Priority, int SpecificityScore, int BaseMinutes, int BufferBeforeMinutes, int BufferAfterMinutes, RuleConditionView Condition, DateTime CreatedAtUtc);
