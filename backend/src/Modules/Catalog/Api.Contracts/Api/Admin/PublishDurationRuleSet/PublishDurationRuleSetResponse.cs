using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.PublishDurationRuleSet;

public sealed class PublishDurationRuleSetResponse : DurationRuleSetResponseBase
{
    public new static PublishDurationRuleSetResponse FromView(DurationRuleSetView view)
        => FromView<PublishDurationRuleSetResponse>(view);
}
