using ErrorOr;

namespace Tailbook.Modules.Catalog.Domain.Aggregates;

public sealed class DurationRuleSet
{
    private readonly List<DurationRule> _rules = [];

    public Guid Id { get; set; }
    public int VersionNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public IReadOnlyCollection<DurationRule> Rules => _rules.AsReadOnly();

    public static DurationRuleSet Create(Guid id, int versionNo, DateTimeOffset validFrom, DateTimeOffset? validTo, DateTimeOffset utcNow)
    {
        return new DurationRuleSet
        {
            Id = id,
            VersionNo = versionNo,
            Status = RuleSetStatusCodes.Draft,
            ValidFrom = validFrom.ToUniversalTime(),
            ValidTo = validTo?.ToUniversalTime(),
            CreatedAt = utcNow.ToUniversalTime()
        };
    }

    public ErrorOr<DurationRule> AddRule(
        Guid offerId,
        int priority,
        int baseMinutes,
        int bufferBeforeMinutes,
        int bufferAfterMinutes,
        Guid? animalTypeId,
        Guid? breedId,
        Guid? breedGroupId,
        Guid? coatTypeId,
        Guid? sizeCategoryId,
        DateTimeOffset utcNow)
    {
        if (!string.Equals(Status, RuleSetStatusCodes.Draft, StringComparison.OrdinalIgnoreCase))
        {
            return CatalogErrors.DurationRuleSetNotDraft;
        }

        if (_rules.Any(x => x.OfferId == offerId && x.HasEquivalentCondition(animalTypeId, breedId, breedGroupId, coatTypeId, sizeCategoryId)))
        {
            return CatalogErrors.DuplicateDurationRule;
        }

        var rule = DurationRule.Create(
            Guid.NewGuid(),
            Id,
            offerId,
            priority,
            baseMinutes,
            bufferBeforeMinutes,
            bufferAfterMinutes,
            animalTypeId,
            breedId,
            breedGroupId,
            coatTypeId,
            sizeCategoryId,
            utcNow);

        _rules.Add(rule);
        return rule;
    }

    public ErrorOr<Success> EnsureCanAddRule()
    {
        if (!string.Equals(Status, RuleSetStatusCodes.Draft, StringComparison.OrdinalIgnoreCase))
        {
            return CatalogErrors.DurationRuleSetNotDraft;
        }

        return Result.Success;
    }

    public ErrorOr<Success> Publish(DateTimeOffset utcNow)
    {
        if (!string.Equals(Status, RuleSetStatusCodes.Draft, StringComparison.OrdinalIgnoreCase))
        {
            return CatalogErrors.PublishDurationRuleSetNotDraft;
        }

        if (_rules.Count == 0)
        {
            return CatalogErrors.DurationRuleSetEmpty;
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
