using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.CreatePriceRule;

public sealed class CreatePriceRuleResponse : PriceRuleResponseBase
{
    public new static CreatePriceRuleResponse FromView(PriceRuleView view)
        => FromView<CreatePriceRuleResponse>(view);
}
