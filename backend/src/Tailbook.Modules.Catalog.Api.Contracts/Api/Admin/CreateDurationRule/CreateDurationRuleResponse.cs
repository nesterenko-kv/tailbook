using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateDurationRule;

public sealed class CreateDurationRuleResponse : DurationRuleResponseBase
{
    public new static CreateDurationRuleResponse FromView(DurationRuleView view)
        => FromView<CreateDurationRuleResponse>(view);
}
