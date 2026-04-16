using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Identity.Application;

public interface IIdentityAccessPolicy
{
    bool CanReadUsers(ICurrentUser currentUser);
    bool CanWriteUsers(ICurrentUser currentUser);
    bool CanReadRoles(ICurrentUser currentUser);
    bool CanAssignRoles(ICurrentUser currentUser);
    bool CanReadAccessAudit(ICurrentUser currentUser);
}
