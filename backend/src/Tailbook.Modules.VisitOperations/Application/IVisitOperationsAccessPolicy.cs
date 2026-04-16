using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.VisitOperations.Application;

public interface IVisitOperationsAccessPolicy
{
    bool CanReadVisits(ICurrentUser currentUser);
    bool CanWriteVisits(ICurrentUser currentUser);
}
