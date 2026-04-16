namespace Tailbook.BuildingBlocks.Abstractions;

public interface IAccessAuditService
{
    Task RecordAsync(string resourceType, string resourceId, string actionCode, Guid? actorUserId, CancellationToken cancellationToken);
}
