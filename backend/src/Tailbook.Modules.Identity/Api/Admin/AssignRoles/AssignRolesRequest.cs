namespace Tailbook.Modules.Identity.Api.Admin.AssignRoles;

public sealed class AssignRolesRequest
{
    public Guid Id { get; set; }
    public IReadOnlyCollection<string> RoleCodes { get; set; } = [];
}
