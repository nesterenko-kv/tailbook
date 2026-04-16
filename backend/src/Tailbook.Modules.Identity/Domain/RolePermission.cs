namespace Tailbook.Modules.Identity.Domain;

public sealed class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
