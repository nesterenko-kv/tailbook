using ErrorOr;

namespace Tailbook.Modules.Catalog.Domain.Aggregates;

public sealed class PriceRuleSet
{
    private readonly List<PriceRule> _rules = [];

    public Guid Id { get; set; }
    public int VersionNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public IReadOnlyCollection<PriceRule> Rules => _rules.AsReadOnly();

    public static PriceRuleSet Create(Guid id, int versionNo, DateTimeOffset validFrom, DateTimeOffset? validTo, DateTimeOffset utcNow)
    {
        return new PriceRuleSet
        {
            Id = id,
            VersionNo = versionNo,
            Status = RuleSetStatusCodes.Draft,
            ValidFrom = validFrom.ToUniversalTime(),
            ValidTo = validTo?.ToUniversalTime(),
            CreatedAt = utcNow.ToUniversalTime()
        };
    }

    public ErrorOr<PriceRule> AddRule(
        Guid offerId,
        int priority,
        decimal fixedAmount,
        string currency,
        Guid? animalTypeId,
        Guid? breedId,
        Guid? breedGroupId,
        Guid? coatTypeId,
        Guid? sizeCategoryId,
        DateTimeOffset utcNow)
    {
        if (!string.Equals(Status, RuleSetStatusCodes.Draft, StringComparison.OrdinalIgnoreCase))
        {
            return CatalogErrors.PriceRuleSetNotDraft;
        }

        if (_rules.Any(x => x.OfferId == offerId && x.HasEquivalentCondition(animalTypeId, breedId, breedGroupId, coatTypeId, sizeCategoryId)))
        {
            return CatalogErrors.DuplicatePriceRule;
        }

        var rule = PriceRule.Create(
            Guid.NewGuid(),
            Id,
            offerId,
            priority,
            fixedAmount,
            currency,
            animalTypeId,
            breedId,
            breedGroupId,
            coatTypeId,
            sizeCategoryId,
            utcNow);
        if (rule.IsError)
        {
            return rule.Errors;
        }

        _rules.Add(rule.Value);
        return rule.Value;
    }

    public ErrorOr<Success> EnsureCanAddRule()
    {
        if (!string.Equals(Status, RuleSetStatusCodes.Draft, StringComparison.OrdinalIgnoreCase))
        {
            return CatalogErrors.PriceRuleSetNotDraft;
        }

        return Result.Success;
    }

    public ErrorOr<Success> Publish(DateTimeOffset utcNow)
    {
        if (!string.Equals(Status, RuleSetStatusCodes.Draft, StringComparison.OrdinalIgnoreCase))
        {
            return CatalogErrors.PublishPriceRuleSetNotDraft;
        }

        if (_rules.Count == 0)
        {
            return CatalogErrors.PriceRuleSetEmpty;
        }

        Status = RuleSetStatusCodes.Published;
        PublishedAt = utcNow.ToUniversalTime();
        return Result.Success;
    }

    public void Archive()
    {
        Status = RuleSetStatusCodes.Archived;
    }
}
