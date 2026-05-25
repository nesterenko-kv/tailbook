using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.CreatePriceRuleSet;

public sealed class CreatePriceRuleSetResponse : PriceRuleSetResponseBase
{
    public static new CreatePriceRuleSetResponse FromView(PriceRuleSetView view)
    {
        return FromView<CreatePriceRuleSetResponse>(view);
    }
}
