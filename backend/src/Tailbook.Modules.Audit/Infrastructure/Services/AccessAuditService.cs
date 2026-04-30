using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Audit.Infrastructure.Services;

public sealed class AccessAuditService(AppDbContext dbContext) : IAccessAuditService
{
    public async Task RecordAsync(string resourceType, string resourceId, string actionCode, Guid? actorUserId, CancellationToken cancellationToken)
    {
        dbContext.Set<AccessAuditEntry>().Add(new AccessAuditEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            ActionCode = actionCode,
            HappenedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
