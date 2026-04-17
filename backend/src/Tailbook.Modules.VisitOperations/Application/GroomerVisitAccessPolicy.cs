using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Identity.Contracts;

namespace Tailbook.Modules.VisitOperations.Application;

public sealed class GroomerVisitAccessPolicy : IGroomerVisitAccessPolicy
{
    public bool CanReadOwnVisits(ICurrentUser currentUser)
    {
        return currentUser.HasPermission(PermissionCodes.GroomerAppAccess)
               && currentUser.HasPermission(PermissionCodes.GroomerVisitsRead);
    }

    public bool CanWriteOwnVisits(ICurrentUser currentUser)
    {
        return currentUser.HasPermission(PermissionCodes.GroomerAppAccess)
               && currentUser.HasPermission(PermissionCodes.GroomerVisitsWrite);
    }
}
