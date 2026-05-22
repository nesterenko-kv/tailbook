using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.ListPriceRuleSets;

public sealed class ListPriceRuleSetsResponse
{
    public PriceRuleSetResponseBase[] Items { get; set; } = [];
}