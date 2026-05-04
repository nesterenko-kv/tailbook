namespace Tailbook.Modules.Catalog.Application.Catalog.Commands;

public sealed record CreatePriceRuleCommand(Guid RuleSetId, Guid OfferId, int Priority, decimal FixedAmount, string Currency, RuleConditionInput Condition);
public sealed record CreateDurationRuleCommand(Guid RuleSetId, Guid OfferId, int Priority, int BaseMinutes, int BufferBeforeMinutes, int BufferAfterMinutes, RuleConditionInput Condition);
