using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.CreatePriceRuleSet;

public sealed class CreatePriceRuleSetResponse : PriceRuleSetResponseBase
{
    public new static CreatePriceRuleSetResponse FromView(PriceRuleSetView view)
        => FromView<CreatePriceRuleSetResponse>(view);
}
