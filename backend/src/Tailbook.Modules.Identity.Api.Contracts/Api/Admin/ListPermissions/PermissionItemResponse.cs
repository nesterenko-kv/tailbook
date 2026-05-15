namespace Tailbook.Modules.Identity.Api.Admin.ListPermissions;

public sealed class PermissionItemResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
