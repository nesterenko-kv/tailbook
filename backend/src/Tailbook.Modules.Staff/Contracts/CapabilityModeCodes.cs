namespace Tailbook.Modules.Staff.Contracts;

public static class CapabilityModeCodes
{
    public const string Allow = "Allow";
    public const string Deny = "Deny";

    public static readonly IReadOnlyCollection<string> All = [Allow, Deny];
}
