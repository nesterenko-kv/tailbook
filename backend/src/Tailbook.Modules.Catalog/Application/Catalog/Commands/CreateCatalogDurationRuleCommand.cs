using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Catalog.Application.Catalog.Commands;

public sealed record CreateCatalogDurationRuleCommand(
    Guid RuleSetId,
    Guid OfferId,
    int Priority,
    int BaseMinutes,
    int BufferBeforeMinutes,
    int BufferAfterMinutes,
    RuleConditionInput Condition) : ICommand<ErrorOr<DurationRuleView>>;
