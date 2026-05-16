using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Audit.Infrastructure.WriteBuffering;

namespace Tailbook.Modules.Audit.Infrastructure.Services;

internal sealed class AccessAuditService(IAuditWriteQueue queue, TimeProvider timeProvider) : IAccessAuditService
{
    public ValueTask RecordAsync(string resourceType, string resourceId, string actionCode, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var item = new AccessAuditWriteItem(
            Guid.NewGuid(),
            actorUserId,
            resourceType,
            resourceId,
            actionCode,
            timeProvider.GetUtcNow());

        return queue.EnqueueAsync(item, cancellationToken);
    }
}
