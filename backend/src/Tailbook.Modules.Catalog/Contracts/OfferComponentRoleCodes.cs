namespace Tailbook.Modules.Catalog.Contracts;

public static class OfferComponentRoleCodes
{
    public const string Included = "Included";
    public const string OptionalOperational = "OptionalOperational";
    public const string Recommended = "Recommended";
    public const string ExcludedByPolicy = "ExcludedByPolicy";

    public static readonly IReadOnlyCollection<string> All =
    [
        Included,
        OptionalOperational,
        Recommended,
        ExcludedByPolicy
    ];
}
