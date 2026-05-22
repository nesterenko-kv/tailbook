namespace Tailbook.Modules.Catalog.Application.Catalog.Commands;

public sealed record CreatePriceRuleContextData(Guid RuleSetId, Guid OfferId, int Priority, decimal FixedAmount, string Currency, RuleConditionInput Condition);
