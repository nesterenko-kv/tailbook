namespace Tailbook.Modules.Catalog.Contracts;

public static class PriceRuleActionTypes
{
    public const string FixedAmount = "FixedAmount";

    public static readonly IReadOnlyCollection<string> All = [FixedAmount];
}
