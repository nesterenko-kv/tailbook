namespace Tailbook.Modules.Identity.Domain.Aggregates;

public sealed class IdentityRole
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
}
