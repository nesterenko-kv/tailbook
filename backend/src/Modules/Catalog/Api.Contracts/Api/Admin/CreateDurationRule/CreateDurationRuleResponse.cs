using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateDurationRule;

public sealed class CreateDurationRuleResponse : DurationRuleResponseBase
{
    public static new CreateDurationRuleResponse FromView(DurationRuleView view)
        => FromView<CreateDurationRuleResponse>(view);
}
