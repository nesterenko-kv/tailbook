using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.PublishPriceRuleSet;

public sealed class PublishPriceRuleSetResponse : PriceRuleSetResponseBase
{
    public new static PublishPriceRuleSetResponse FromView(PriceRuleSetView view)
        => FromView<PublishPriceRuleSetResponse>(view);
}
