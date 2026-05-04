namespace Tailbook.Modules.Catalog.Application.Catalog.Models;

public sealed record RuleConditionInput(Guid? AnimalTypeId, Guid? BreedId, Guid? BreedGroupId, Guid? CoatTypeId, Guid? SizeCategoryId);

public sealed record RuleConditionView(Guid? AnimalTypeId, Guid? BreedId, Guid? BreedGroupId, Guid? CoatTypeId, Guid? SizeCategoryId);
public sealed record PriceRuleSetView(Guid Id, int VersionNo, string Status, DateTime ValidFromUtc, DateTime? ValidToUtc, DateTime CreatedAtUtc, DateTime? PublishedAtUtc, IReadOnlyCollection<PriceRuleView> Rules);
public sealed record PriceRuleView(Guid Id, Guid RuleSetId, Guid OfferId, string OfferCode, string OfferDisplayName, int Priority, int SpecificityScore, string ActionType, decimal FixedAmount, string Currency, RuleConditionView Condition, DateTime CreatedAtUtc);
public sealed record DurationRuleSetView(Guid Id, int VersionNo, string Status, DateTime ValidFromUtc, DateTime? ValidToUtc, DateTime CreatedAtUtc, DateTime? PublishedAtUtc, IReadOnlyCollection<DurationRuleView> Rules);
public sealed record DurationRuleView(Guid Id, Guid RuleSetId, Guid OfferId, string OfferCode, string OfferDisplayName, int Priority, int SpecificityScore, int BaseMinutes, int BufferBeforeMinutes, int BufferAfterMinutes, RuleConditionView Condition, DateTime CreatedAtUtc);
