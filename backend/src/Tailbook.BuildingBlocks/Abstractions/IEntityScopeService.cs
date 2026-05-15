using ErrorOr;

namespace Tailbook.BuildingBlocks.Abstractions;

public interface IEntityScopeService
{
    Task<ErrorOr<Success>> VerifyAccessAsync(string resourceType, string resourceId, Guid? actorUserId, CancellationToken cancellationToken);
}
