using ErrorOr;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Catalog.Domain.Events;

namespace Tailbook.Modules.Catalog.Domain.Aggregates;

public sealed class PriceRuleSet : AggregateRoot
{
    private readonly List<PriceRule> _rules = [];

    private PriceRuleSet()
    {
    }

    public int VersionNo { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidTo { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public IReadOnlyCollection<PriceRule> Rules => _rules.AsReadOnly();

    public static PriceRuleSet Create(Guid id, int versionNo, DateTimeOffset validFrom, DateTimeOffset? validTo, DateTimeOffset utcNow)
    {
        var ruleSet = new PriceRuleSet
        {
            Id = id,
            VersionNo = versionNo,
            Status = RuleSetStatusCodes.Draft,
            ValidFrom = validFrom.ToUniversalTime(),
            ValidTo = validTo?.ToUniversalTime(),
            CreatedAt = utcNow.ToUniversalTime()
        };

        ruleSet.RaiseDomainEvent(new PriceRuleSetCreatedDomainEvent(
            Guid.NewGuid(),
            utcNow.ToUniversalTime(),
            ruleSet.Id,
            ruleSet.VersionNo,
            ruleSet.Status,
            ruleSet.ValidFrom,
            ruleSet.ValidTo));

        return ruleSet;
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

        RaiseDomainEvent(new PriceRuleAddedDomainEvent(
            Guid.NewGuid(),
            utcNow.ToUniversalTime(),
            Id,
            rule.Value.Id,
            offerId,
            priority,
            fixedAmount,
            currency));

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

        RaiseDomainEvent(new PriceRuleSetPublishedDomainEvent(
            Guid.NewGuid(),
            utcNow.ToUniversalTime(),
            Id,
            VersionNo,
            Status,
            PublishedAt.Value));

        return Result.Success;
    }

    public void Archive()
    {
        Status = RuleSetStatusCodes.Archived;

        RaiseDomainEvent(new PriceRuleSetArchivedDomainEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Id,
            VersionNo,
            Status));
    }
}
