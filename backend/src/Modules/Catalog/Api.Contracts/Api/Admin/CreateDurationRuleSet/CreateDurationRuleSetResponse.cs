using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateDurationRuleSet;

public sealed class CreateDurationRuleSetResponse : DurationRuleSetResponseBase
{
    public static new CreateDurationRuleSetResponse FromView(DurationRuleSetView view)
    {
        return FromView<CreateDurationRuleSetResponse>(view);
    }
}
