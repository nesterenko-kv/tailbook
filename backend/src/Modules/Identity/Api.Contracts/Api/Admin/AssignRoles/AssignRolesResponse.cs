namespace Tailbook.Modules.Identity.Api.Admin.AssignRoles;

public sealed class AssignRolesResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; set; } = [];
    public IReadOnlyCollection<string> Permissions { get; set; } = [];
}
