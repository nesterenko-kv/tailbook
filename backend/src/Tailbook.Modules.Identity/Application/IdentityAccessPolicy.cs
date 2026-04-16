using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Identity.Contracts;

namespace Tailbook.Modules.Identity.Application;

public sealed class IdentityAccessPolicy : IIdentityAccessPolicy
{
    public bool CanReadUsers(ICurrentUser currentUser) => currentUser.HasPermission(PermissionCodes.IamUsersRead);

    public bool CanWriteUsers(ICurrentUser currentUser) => currentUser.HasPermission(PermissionCodes.IamUsersWrite);

    public bool CanReadRoles(ICurrentUser currentUser) => currentUser.HasPermission(PermissionCodes.IamRolesRead);

    public bool CanAssignRoles(ICurrentUser currentUser) => currentUser.HasPermission(PermissionCodes.IamRolesAssign);

    public bool CanReadAccessAudit(ICurrentUser currentUser) => currentUser.HasPermission(PermissionCodes.AuditAccessRead);
}
