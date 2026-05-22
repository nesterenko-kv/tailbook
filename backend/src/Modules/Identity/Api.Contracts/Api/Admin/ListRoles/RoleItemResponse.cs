namespace Tailbook.Modules.Identity.Api.Admin.ListRoles;

public sealed class RoleItemResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public IReadOnlyCollection<string> PermissionCodes { get; set; } = [];
}
