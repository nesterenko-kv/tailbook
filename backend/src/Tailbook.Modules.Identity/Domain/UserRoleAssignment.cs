namespace Tailbook.Modules.Identity.Domain;

public sealed class UserRoleAssignment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public string ScopeType { get; set; } = string.Empty;
    public string? ScopeId { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public Guid? AssignedByUserId { get; set; }
}
