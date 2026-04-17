using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.VisitOperations.Application;

public interface IGroomerVisitAccessPolicy
{
    bool CanReadOwnVisits(ICurrentUser currentUser);
    bool CanWriteOwnVisits(ICurrentUser currentUser);
}
