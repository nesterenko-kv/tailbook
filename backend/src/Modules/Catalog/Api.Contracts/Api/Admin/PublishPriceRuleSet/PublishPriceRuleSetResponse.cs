using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.PublishPriceRuleSet;

public sealed class PublishPriceRuleSetResponse : PriceRuleSetResponseBase
{
    public static new PublishPriceRuleSetResponse FromView(PriceRuleSetView view)
    {
        return FromView<PublishPriceRuleSetResponse>(view);
    }
}
