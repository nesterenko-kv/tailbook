namespace Tailbook.BuildingBlocks.Abstractions;

public interface IAuditTrailService
{
    ValueTask RecordAsync(string moduleCode, string entityType, string entityId, string actionCode, Guid? actorUserId, string? beforeJson, string? afterJson, CancellationToken cancellationToken);
}
