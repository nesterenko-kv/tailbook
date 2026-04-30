namespace Tailbook.Modules.Audit.Application.AccessAuditEntries.Models;

public sealed record AccessAuditEntryReadModel(
    Guid Id,
    Guid? ActorUserId,
    string ResourceType,
    string ResourceId,
    string ActionCode,
    DateTime HappenedAtUtc);
