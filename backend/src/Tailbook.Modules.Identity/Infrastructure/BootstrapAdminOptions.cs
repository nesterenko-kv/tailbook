namespace Tailbook.Modules.Identity.Infrastructure;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string Email { get; init; } = "admin@tailbook.local";
    public string Password { get; init; } = "MyV3ryC00lAdminP@ss";
    public string DisplayName { get; init; } = "Bootstrap Administrator";
}
