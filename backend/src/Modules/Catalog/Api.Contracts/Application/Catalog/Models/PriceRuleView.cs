namespace Tailbook.Modules.Catalog.Application.Catalog.Models;

public sealed record PriceRuleView(Guid Id, Guid RuleSetId, Guid OfferId, string OfferCode, string OfferDisplayName, int Priority, int SpecificityScore, string ActionType, decimal FixedAmount, string Currency, RuleConditionView Condition, DateTimeOffset CreatedAt);