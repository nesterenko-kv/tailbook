using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.VisitOperations.Application;

public sealed class VisitOperationsAccessPolicy : IVisitOperationsAccessPolicy
{
    private const string VisitReadPermission = "visit.read";
    private const string VisitWritePermission = "visit.write";

    public bool CanReadVisits(ICurrentUser currentUser) => currentUser.HasPermission(VisitReadPermission);
    public bool CanWriteVisits(ICurrentUser currentUser) => currentUser.HasPermission(VisitWritePermission);
}
