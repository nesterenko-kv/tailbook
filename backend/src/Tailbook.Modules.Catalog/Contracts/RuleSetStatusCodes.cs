namespace Tailbook.Modules.Catalog.Contracts;

public static class RuleSetStatusCodes
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Archived = "Archived";

    public static readonly IReadOnlyCollection<string> All = [Draft, Published, Archived];
}
