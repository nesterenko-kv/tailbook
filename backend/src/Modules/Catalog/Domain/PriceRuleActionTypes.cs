namespace Tailbook.Modules.Catalog.Domain;

public static class PriceRuleActionTypes
{
    public const string FixedAmount = "FixedAmount";

    public static readonly IReadOnlyCollection<string> All = [FixedAmount];
}
