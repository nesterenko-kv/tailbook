namespace Tailbook.Modules.Catalog.Application.Catalog.Models;

public sealed record DurationRuleView(Guid Id, Guid RuleSetId, Guid OfferId, string OfferCode, string OfferDisplayName, int Priority, int SpecificityScore, int BaseMinutes, int BufferBeforeMinutes, int BufferAfterMinutes, RuleConditionView Condition, DateTimeOffset CreatedAt);