using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.ListDurationRuleSets;

public sealed class ListDurationRuleSetsResponse
{
    public DurationRuleSetResponseBase[] Items { get; set; } = [];
}