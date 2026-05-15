using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Audit.Infrastructure.WriteBuffering;

namespace Tailbook.Modules.Audit.Infrastructure.Services;

internal sealed class AuditTrailService(IAuditWriteQueue queue, TimeProvider timeProvider) : IAuditTrailService
{
    public Task RecordAsync(string moduleCode, string entityType, string entityId, string actionCode, Guid? actorUserId, string? beforeJson, string? afterJson, CancellationToken cancellationToken)
    {
        var item = new AuditTrailWriteItem(
            Guid.NewGuid(),
            actorUserId,
            moduleCode,
            entityType,
            entityId,
            actionCode,
            timeProvider.GetUtcNow(),
            beforeJson,
            afterJson);

        return queue.EnqueueAsync(item, cancellationToken).AsTask();
    }
}
