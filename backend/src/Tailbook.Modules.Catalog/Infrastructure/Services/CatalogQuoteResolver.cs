using System.Text.Json;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Catalog.Infrastructure.Services;

public sealed class CatalogQuoteResolver(AppDbContext dbContext, TimeProvider timeProvider, IDistributedCache cache) : ICatalogQuoteResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<ErrorOr<CatalogQuoteResolution>> ResolveAsync(PetQuoteProfile pet, IReadOnlyCollection<QuotePreviewCatalogItem> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return Error.Validation("Catalog.QuoteItemRequired", "At least one commercial item is required for quote preview.");
        }

        var utcNow = timeProvider.GetUtcNow();

        var (priceRuleSet, durationRuleSet) = await LoadRuleSetsAsync(utcNow, cancellationToken);
        if (priceRuleSet is null)
        {
            return Error.Validation("Catalog.PublishedPriceRuleSetRequired", "No published price rule set is available.");
        }

        if (durationRuleSet is null)
        {
            return Error.Validation("Catalog.PublishedDurationRuleSetRequired", "No published duration rule set is available.");
        }

        var offerIds = items.Select(x => x.OfferId).Distinct().ToArray();
        var offers = await dbContext.Set<CommercialOffer>()
            .AsNoTracking()
            .Where(x => offerIds.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (offers.Count != offerIds.Length)
        {
            return Error.NotFound("Catalog.OfferNotFound", "One or more requested offers do not exist or are inactive.");
        }

        var publishedOfferVersions = await dbContext.Set<OfferVersion>()
            .AsNoTracking()
            .Where(x => offerIds.Contains(x.OfferId)
                        && x.Status == OfferVersionStatusCodes.Published
                        && x.ValidFrom <= utcNow
                        && (x.ValidTo == null || x.ValidTo >= utcNow))
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

            var matchingPriceRule = priceRuleSet.PriceRules
                .Where(x => x.OfferId == offer.Id)
                .Select(x => new { Rule = x, Condition = priceRuleSet.PriceConditions[x.Id] })
                .Where(x => Matches(pet, x.Condition))
                .OrderByDescending(x => x.Rule.SpecificityScore)
                .ThenBy(x => x.Rule.Priority)
                .ThenBy(x => x.Rule.CreatedAt)
                .FirstOrDefault();

            if (matchingPriceRule is null)
            {
                return Error.Validation("Catalog.MatchingPriceRuleRequired", $"No matching price rule exists for offer '{offer.DisplayName}'.");
            }

            var matchingDurationRule = durationRuleSet.DurationRules
                .Where(x => x.OfferId == offer.Id)
                .Select(x => new { Rule = x, Condition = durationRuleSet.DurationConditions[x.Id] })
                .Where(x => Matches(pet, x.Condition))
                .OrderByDescending(x => x.Rule.SpecificityScore)
                .ThenBy(x => x.Rule.Priority)
                .ThenBy(x => x.Rule.CreatedAt)
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

    private async Task<(CachedPriceRuleSetData? PriceRuleSet, CachedDurationRuleSetData? DurationRuleSet)> LoadRuleSetsAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var priceRuleSet = await TryGetCachedAsync<CachedPriceRuleSetData>("catalog:price-rule-set:active", cancellationToken);
        var durationRuleSet = await TryGetCachedAsync<CachedDurationRuleSetData>("catalog:duration-rule-set:active", cancellationToken);

        if (priceRuleSet is null || !priceRuleSet.IsValid(utcNow))
        {
            priceRuleSet = await LoadPriceRuleSetAsync(utcNow, cancellationToken);
            if (priceRuleSet is not null)
            {
                await SetCachedAsync("catalog:price-rule-set:active", priceRuleSet, cancellationToken);
            }
        }

        if (durationRuleSet is null || !durationRuleSet.IsValid(utcNow))
        {
            durationRuleSet = await LoadDurationRuleSetAsync(utcNow, cancellationToken);
            if (durationRuleSet is not null)
            {
                await SetCachedAsync("catalog:duration-rule-set:active", durationRuleSet, cancellationToken);
            }
        }

        return (priceRuleSet, durationRuleSet);
    }

    private async Task<CachedPriceRuleSetData?> LoadPriceRuleSetAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var priceRuleSet = await dbContext.Set<PriceRuleSet>()
            .AsNoTracking()
            .Include(x => x.Rules)
            .ThenInclude(x => x.Condition)
            .Where(x => x.Status == RuleSetStatusCodes.Published && x.ValidFrom <= utcNow && (x.ValidTo == null || x.ValidTo >= utcNow))
            .OrderByDescending(x => x.ValidFrom)
            .ThenByDescending(x => x.VersionNo)
            .FirstOrDefaultAsync(cancellationToken);

        if (priceRuleSet is null)
        {
            return null;
        }

        var priceRules = priceRuleSet.Rules.Select(r => new CachedPriceRuleData(
            r.Id, r.RuleSetId, r.OfferId, r.Priority, r.SpecificityScore,
            r.FixedAmount, r.Currency, r.CreatedAt,
            new CachedConditionData(
                r.Condition.AnimalTypeId, r.Condition.BreedId, r.Condition.BreedGroupId,
                r.Condition.CoatTypeId, r.Condition.SizeCategoryId))).ToList();

        var priceConditions = priceRules.ToDictionary(r => r.Id, r => r.Condition);

        return new CachedPriceRuleSetData(priceRuleSet.Id, priceRuleSet.VersionNo, priceRuleSet.ValidFrom, priceRuleSet.ValidTo, priceRules, priceConditions);
    }

    private async Task<CachedDurationRuleSetData?> LoadDurationRuleSetAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var durationRuleSet = await dbContext.Set<DurationRuleSet>()
            .AsNoTracking()
            .Include(x => x.Rules)
            .ThenInclude(x => x.Condition)
            .Where(x => x.Status == RuleSetStatusCodes.Published && x.ValidFrom <= utcNow && (x.ValidTo == null || x.ValidTo >= utcNow))
            .OrderByDescending(x => x.ValidFrom)
            .ThenByDescending(x => x.VersionNo)
            .FirstOrDefaultAsync(cancellationToken);

        if (durationRuleSet is null)
        {
            return null;
        }

        var durationRules = durationRuleSet.Rules.Select(r => new CachedDurationRuleData(
            r.Id, r.RuleSetId, r.OfferId, r.Priority, r.SpecificityScore,
            r.BaseMinutes, r.BufferBeforeMinutes, r.BufferAfterMinutes, r.CreatedAt,
            new CachedConditionData(
                r.Condition.AnimalTypeId, r.Condition.BreedId, r.Condition.BreedGroupId,
                r.Condition.CoatTypeId, r.Condition.SizeCategoryId))).ToList();

        var durationConditions = durationRules.ToDictionary(r => r.Id, r => r.Condition);

        return new CachedDurationRuleSetData(durationRuleSet.Id, durationRuleSet.VersionNo, durationRuleSet.ValidFrom, durationRuleSet.ValidTo, durationRules, durationConditions);
    }

    private async Task<T?> TryGetCachedAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        var data = await cache.GetStringAsync(key, cancellationToken);
        return data is null ? null : JsonSerializer.Deserialize<T>(data, JsonOptions);
    }

    private async Task SetCachedAsync<T>(string key, T value, CancellationToken cancellationToken)
    {
        var serialized = JsonSerializer.Serialize(value, JsonOptions);
        await cache.SetStringAsync(key, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        }, cancellationToken);
    }

    private static bool Matches(PetQuoteProfile pet, CachedConditionData condition)
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

    private static string BuildPriceLabel(string offerDisplayName, PetQuoteProfile pet, CachedConditionData condition)
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

    internal sealed record CachedPriceRuleSetData(
        Guid Id,
        int VersionNo,
        DateTimeOffset ValidFrom,
        DateTimeOffset? ValidTo,
        List<CachedPriceRuleData> PriceRules,
        Dictionary<Guid, CachedConditionData> PriceConditions)
    {
        public bool IsValid(DateTimeOffset utcNow)
            => ValidFrom <= utcNow && (ValidTo is null || ValidTo >= utcNow);
    }

    internal sealed record CachedDurationRuleSetData(
        Guid Id,
        int VersionNo,
        DateTimeOffset ValidFrom,
        DateTimeOffset? ValidTo,
        List<CachedDurationRuleData> DurationRules,
        Dictionary<Guid, CachedConditionData> DurationConditions)
    {
        public bool IsValid(DateTimeOffset utcNow)
            => ValidFrom <= utcNow && (ValidTo is null || ValidTo >= utcNow);
    }

    internal sealed record CachedPriceRuleData(
        Guid Id,
        Guid RuleSetId,
        Guid OfferId,
        int Priority,
        int SpecificityScore,
        decimal FixedAmount,
        string Currency,
        DateTimeOffset CreatedAt,
        CachedConditionData Condition);

    internal sealed record CachedDurationRuleData(
        Guid Id,
        Guid RuleSetId,
        Guid OfferId,
        int Priority,
        int SpecificityScore,
        int BaseMinutes,
        int BufferBeforeMinutes,
        int BufferAfterMinutes,
        DateTimeOffset CreatedAt,
        CachedConditionData Condition);

    internal sealed record CachedConditionData(
        Guid? AnimalTypeId,
        Guid? BreedId,
        Guid? BreedGroupId,
        Guid? CoatTypeId,
        Guid? SizeCategoryId);
}
