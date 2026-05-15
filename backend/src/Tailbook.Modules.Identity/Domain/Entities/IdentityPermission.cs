namespace Tailbook.Modules.Identity.Domain.Entities;

public sealed class IdentityPermission
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
