using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Audit.Domain;

namespace Tailbook.Modules.Audit.Application;

public sealed class AuditTrailService(AppDbContext dbContext) : IAuditTrailService
{
    public async Task RecordAsync(string moduleCode, string entityType, string entityId, string actionCode, Guid? actorUserId, string? beforeJson, string? afterJson, CancellationToken cancellationToken)
    {
        dbContext.Set<AuditEntry>().Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            ModuleCode = moduleCode,
            EntityType = entityType,
            EntityId = entityId,
            ActionCode = actionCode,
            ActorUserId = actorUserId,
            HappenedAtUtc = DateTime.UtcNow,
            BeforeJson = beforeJson,
            AfterJson = afterJson
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
