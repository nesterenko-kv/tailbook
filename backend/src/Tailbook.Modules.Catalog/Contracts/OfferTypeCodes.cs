namespace Tailbook.Modules.Catalog.Contracts;

public static class OfferTypeCodes
{
    public const string Package = "Package";
    public const string StandaloneService = "StandaloneService";
    public const string AddOn = "AddOn";

    public static readonly IReadOnlyCollection<string> All =
    [
        Package,
        StandaloneService,
        AddOn
    ];
}
