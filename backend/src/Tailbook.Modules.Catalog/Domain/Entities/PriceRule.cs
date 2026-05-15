using ErrorOr;

namespace Tailbook.Modules.Catalog.Domain.Entities;

public sealed class PriceRule
{
    public Guid Id { get; set; }
    public Guid RuleSetId { get; set; }
    public Guid OfferId { get; set; }
    public int Priority { get; set; }
    public int SpecificityScore { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public decimal FixedAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public PriceRuleCondition Condition { get; set; } = null!;

    internal static ErrorOr<PriceRule> Create(
        Guid id,
        Guid ruleSetId,
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
        var normalizedCurrency = NormalizeCurrency(currency);
        if (normalizedCurrency.IsError)
        {
            return normalizedCurrency.Errors;
        }

        var condition = PriceRuleCondition.Create(Guid.NewGuid(), id, animalTypeId, breedId, breedGroupId, coatTypeId, sizeCategoryId);
        return new PriceRule
        {
            Id = id,
            RuleSetId = ruleSetId,
            OfferId = offerId,
            Priority = priority,
            SpecificityScore = condition.SpecificityScore,
            ActionType = PriceRuleActionTypes.FixedAmount,
            FixedAmount = fixedAmount,
            Currency = normalizedCurrency.Value,
            CreatedAt = utcNow.ToUniversalTime(),
            Condition = condition
        };
    }

    internal bool HasEquivalentCondition(Guid? animalTypeId, Guid? breedId, Guid? breedGroupId, Guid? coatTypeId, Guid? sizeCategoryId)
    {
        return Condition.Matches(animalTypeId, breedId, breedGroupId, coatTypeId, sizeCategoryId);
    }

    private static ErrorOr<string> NormalizeCurrency(string currency)
    {
        var normalized = currency.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return CatalogErrors.CurrencyRequired;
        }

        return normalized;
    }
}
