using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.PublishDurationRuleSet;

public sealed class PublishDurationRuleSetResponse : DurationRuleSetResponseBase
{
    public static new PublishDurationRuleSetResponse FromView(DurationRuleSetView view)
    {
        return FromView<PublishDurationRuleSetResponse>(view);
    }
}
