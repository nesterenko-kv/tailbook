using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Catalog.Contracts;

namespace Tailbook.Modules.Catalog.Infrastructure.Services;

public sealed class CatalogQuoteResolver(AppDbContext dbContext) : ICatalogQuoteResolver
{
    public async Task<ErrorOr<CatalogQuoteResolution>> ResolveAsync(PetQuoteProfile pet, IReadOnlyCollection<QuotePreviewCatalogItem> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return Error.Validation("Catalog.QuoteItemRequired", "At least one commercial item is required for quote preview.");
        }

        var utcNow = DateTime.UtcNow;

        var priceRuleSet = await dbContext.Set<PriceRuleSet>()
            .Where(x => x.Status == RuleSetStatusCodes.Published && x.ValidFromUtc <= utcNow && (x.ValidToUtc == null || x.ValidToUtc >= utcNow))
            .OrderByDescending(x => x.ValidFromUtc)
            .ThenByDescending(x => x.VersionNo)
            .FirstOrDefaultAsync(cancellationToken);
        if (priceRuleSet is null)
        {
            return Error.Validation("Catalog.PublishedPriceRuleSetRequired", "No published price rule set is available.");
        }

        var durationRuleSet = await dbContext.Set<DurationRuleSet>()
            .Where(x => x.Status == RuleSetStatusCodes.Published && x.ValidFromUtc <= utcNow && (x.ValidToUtc == null || x.ValidToUtc >= utcNow))
            .OrderByDescending(x => x.ValidFromUtc)
            .ThenByDescending(x => x.VersionNo)
            .FirstOrDefaultAsync(cancellationToken);
        if (durationRuleSet is null)
        {
            return Error.Validation("Catalog.PublishedDurationRuleSetRequired", "No published duration rule set is available.");
        }

        var offerIds = items.Select(x => x.OfferId).Distinct().ToArray();
        var offers = await dbContext.Set<CommercialOffer>()
            .Where(x => offerIds.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (offers.Count != offerIds.Length)
        {
            return Error.NotFound("Catalog.OfferNotFound", "One or more requested offers do not exist or are inactive.");
        }

        var publishedOfferVersions = await dbContext.Set<OfferVersion>()
            .Where(x => offerIds.Contains(x.OfferId)
                        && x.Status == OfferVersionStatusCodes.Published
                        && x.ValidFromUtc <= utcNow
                        && (x.ValidToUtc == null || x.ValidToUtc >= utcNow))
            .GroupBy(x => x.OfferId)
            .Select(x => x.OrderByDescending(v => v.VersionNo).First())
            .ToDictionaryAsync(x => x.OfferId, cancellationToken);

        foreach (var offerId in offerIds)
        {
            if (!publishedOfferVersions.ContainsKey(offerId))
            {
                return Error.Validation("Catalog.PublishedOfferVersionRequired", "Each requested offer must have a published offer version before quote preview.");
            }
        }

        var priceRules = await dbContext.Set<PriceRule>()
            .Where(x => x.RuleSetId == priceRuleSet.Id && offerIds.Contains(x.OfferId))
            .ToListAsync(cancellationToken);
        var priceConditions = await dbContext.Set<PriceRuleCondition>()
            .Where(x => priceRules.Select(r => r.Id).Contains(x.PriceRuleId))
            .ToDictionaryAsync(x => x.PriceRuleId, cancellationToken);

        var durationRules = await dbContext.Set<DurationRule>()
            .Where(x => x.RuleSetId == durationRuleSet.Id && offerIds.Contains(x.OfferId))
            .ToListAsync(cancellationToken);
        var durationConditions = await dbContext.Set<DurationRuleCondition>()
            .Where(x => durationRules.Select(r => r.Id).Contains(x.DurationRuleId))
            .ToDictionaryAsync(x => x.DurationRuleId, cancellationToken);

        var priceLines = new List<CatalogQuotePriceLine>();
        var durationLines = new List<CatalogQuoteDurationLine>();
        var resolvedItems = new List<CatalogResolvedQuoteItem>();
        var totalAmount = 0m;
        var totalServiceMinutes = 0;
        var totalReservedMinutes = 0;
        var priceSequence = 1;
        var durationSequence = 1;
        string? currency = null;

        foreach (var requestedItem in items)
        {
            var offer = offers[requestedItem.OfferId];
            if (!string.IsNullOrWhiteSpace(requestedItem.ItemType)
                && !string.Equals(requestedItem.ItemType, offer.OfferType, StringComparison.OrdinalIgnoreCase))
            {
                return Error.Validation("Catalog.OfferTypeMismatch", $"Requested item type '{requestedItem.ItemType}' does not match offer type '{offer.OfferType}'.");
            }

            var offerVersion = publishedOfferVersions[offer.Id];

            var matchingPriceRule = priceRules
                .Where(x => x.OfferId == offer.Id)
                .Select(x => new { Rule = x, Condition = priceConditions[x.Id] })
                .Where(x => Matches(pet, x.Condition))
                .OrderByDescending(x => x.Rule.SpecificityScore)
                .ThenBy(x => x.Rule.Priority)
                .ThenBy(x => x.Rule.CreatedAtUtc)
                .FirstOrDefault();

            if (matchingPriceRule is null)
            {
                return Error.Validation("Catalog.MatchingPriceRuleRequired", $"No matching price rule exists for offer '{offer.DisplayName}'.");
            }

            var matchingDurationRule = durationRules
                .Where(x => x.OfferId == offer.Id)
                .Select(x => new { Rule = x, Condition = durationConditions[x.Id] })
                .Where(x => Matches(pet, x.Condition))
                .OrderByDescending(x => x.Rule.SpecificityScore)
                .ThenBy(x => x.Rule.Priority)
                .ThenBy(x => x.Rule.CreatedAtUtc)
                .FirstOrDefault();

            if (matchingDurationRule is null)
            {
                return Error.Validation("Catalog.MatchingDurationRuleRequired", $"No matching duration rule exists for offer '{offer.DisplayName}'.");
            }

            currency ??= matchingPriceRule.Rule.Currency;
            if (!string.Equals(currency, matchingPriceRule.Rule.Currency, StringComparison.OrdinalIgnoreCase))
            {
                return Error.Validation("Catalog.MixedQuoteCurrencies", "All matched price rules in one quote preview must resolve to the same currency.");
            }

            var amount = matchingPriceRule.Rule.FixedAmount;
            var serviceMinutes = matchingDurationRule.Rule.BaseMinutes;
            var reservedMinutes = checked(matchingDurationRule.Rule.BaseMinutes + matchingDurationRule.Rule.BufferBeforeMinutes + matchingDurationRule.Rule.BufferAfterMinutes);

            priceLines.Add(new CatalogQuotePriceLine(
                offer.Id,
                offerVersion.Id,
                "BaseOfferRule",
                BuildPriceLabel(offer.DisplayName, pet, matchingPriceRule.Condition),
                amount,
                matchingPriceRule.Rule.Id,
                priceSequence++));

            durationLines.Add(new CatalogQuoteDurationLine(
                offer.Id,
                offerVersion.Id,
                "BaseDurationRule",
                $"{offer.DisplayName} - base service time",
                serviceMinutes,
                matchingDurationRule.Rule.Id,
                durationSequence++));

            if (matchingDurationRule.Rule.BufferBeforeMinutes > 0)
            {
                durationLines.Add(new CatalogQuoteDurationLine(
                    offer.Id,
                    offerVersion.Id,
                    "BufferBefore",
                    $"{offer.DisplayName} - setup / intake buffer",
                    matchingDurationRule.Rule.BufferBeforeMinutes,
                    matchingDurationRule.Rule.Id,
                    durationSequence++));
            }

            if (matchingDurationRule.Rule.BufferAfterMinutes > 0)
            {
                durationLines.Add(new CatalogQuoteDurationLine(
                    offer.Id,
                    offerVersion.Id,
                    "BufferAfter",
                    $"{offer.DisplayName} - cleanup / handoff buffer",
                    matchingDurationRule.Rule.BufferAfterMinutes,
                    matchingDurationRule.Rule.Id,
                    durationSequence++));
            }

            totalAmount += amount;
            totalServiceMinutes += serviceMinutes;
            totalReservedMinutes += reservedMinutes;

            resolvedItems.Add(new CatalogResolvedQuoteItem(
                offer.Id,
                offerVersion.Id,
                offer.Code,
                offer.OfferType,
                offer.DisplayName,
                amount,
                serviceMinutes,
                reservedMinutes));
        }

        return new CatalogQuoteResolution(
            priceRuleSet.Id,
            durationRuleSet.Id,
            currency ?? "UAH",
            totalAmount,
            totalServiceMinutes,
            totalReservedMinutes,
            priceLines,
            durationLines,
            resolvedItems);
    }

    private static bool Matches(PetQuoteProfile pet, PriceRuleCondition condition)
    {
        if (condition.AnimalTypeId is not null && condition.AnimalTypeId != pet.AnimalTypeId)
        {
            return false;
        }

        if (condition.BreedId is not null && condition.BreedId != pet.BreedId)
        {
            return false;
        }

        if (condition.BreedGroupId is not null && condition.BreedGroupId != pet.BreedGroupId)
        {
            return false;
        }

        if (condition.CoatTypeId is not null && condition.CoatTypeId != pet.CoatTypeId)
        {
            return false;
        }

        if (condition.SizeCategoryId is not null && condition.SizeCategoryId != pet.SizeCategoryId)
        {
            return false;
        }

        return true;
    }

    private static bool Matches(PetQuoteProfile pet, DurationRuleCondition condition)
    {
        if (condition.AnimalTypeId is not null && condition.AnimalTypeId != pet.AnimalTypeId)
        {
            return false;
        }

        if (condition.BreedId is not null && condition.BreedId != pet.BreedId)
        {
            return false;
        }

        if (condition.BreedGroupId is not null && condition.BreedGroupId != pet.BreedGroupId)
        {
            return false;
        }

        if (condition.CoatTypeId is not null && condition.CoatTypeId != pet.CoatTypeId)
        {
            return false;
        }

        if (condition.SizeCategoryId is not null && condition.SizeCategoryId != pet.SizeCategoryId)
        {
            return false;
        }

        return true;
    }

    private static string BuildPriceLabel(string offerDisplayName, PetQuoteProfile pet, PriceRuleCondition condition)
    {
        if (condition.BreedId is not null)
        {
            return $"{offerDisplayName} - {pet.BreedName}";
        }

        if (condition.BreedGroupId is not null && !string.IsNullOrWhiteSpace(pet.BreedGroupName))
        {
            return $"{offerDisplayName} - {pet.BreedGroupName}";
        }

        if (condition.CoatTypeId is not null && !string.IsNullOrWhiteSpace(pet.CoatTypeName))
        {
            return $"{offerDisplayName} - {pet.CoatTypeName}";
        }

        if (condition.SizeCategoryId is not null && !string.IsNullOrWhiteSpace(pet.SizeCategoryName))
        {
            return $"{offerDisplayName} - {pet.SizeCategoryName}";
        }

        return $"{offerDisplayName} - {pet.AnimalTypeName}";
    }
}
