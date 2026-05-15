using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateDurationRuleSet;

public sealed class CreateDurationRuleSetResponse : DurationRuleSetResponseBase
{
    public new static CreateDurationRuleSetResponse FromView(DurationRuleSetView view)
        => FromView<CreateDurationRuleSetResponse>(view);
}
