namespace Tailbook.Modules.Catalog.Infrastructure.Services;

internal static class CatalogRuleViewMapper
{
    public static PriceRuleView ToView(PriceRule rule, PriceRuleCondition? condition, CommercialOffer offer)
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
            ToConditionView(
                condition?.AnimalTypeId,
                condition?.BreedId,
                condition?.BreedGroupId,
                condition?.CoatTypeId,
                condition?.SizeCategoryId),
            rule.CreatedAt);
    }

    public static DurationRuleView ToView(DurationRule rule, DurationRuleCondition? condition, CommercialOffer offer)
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
            ToConditionView(
                condition?.AnimalTypeId,
                condition?.BreedId,
                condition?.BreedGroupId,
                condition?.CoatTypeId,
                condition?.SizeCategoryId),
            rule.CreatedAt);
    }

    private static RuleConditionView ToConditionView(
        Guid? animalTypeId,
        Guid? breedId,
        Guid? breedGroupId,
        Guid? coatTypeId,
        Guid? sizeCategoryId)
    {
        return new RuleConditionView(animalTypeId, breedId, breedGroupId, coatTypeId, sizeCategoryId);
    }
}
