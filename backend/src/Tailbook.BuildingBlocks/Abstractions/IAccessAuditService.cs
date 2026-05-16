namespace Tailbook.BuildingBlocks.Abstractions;

public interface IAccessAuditService
{
    ValueTask RecordAsync(string resourceType, string resourceId, string actionCode, Guid? actorUserId, CancellationToken cancellationToken);
}
