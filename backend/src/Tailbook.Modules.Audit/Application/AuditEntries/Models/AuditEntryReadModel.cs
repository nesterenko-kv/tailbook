namespace Tailbook.Modules.Audit.Application.AuditEntries.Models;

public sealed record AuditEntryReadModel(
    Guid Id,
    string ModuleCode,
    string EntityType,
    string EntityId,
    string ActionCode,
    Guid? ActorUserId,
    DateTimeOffset HappenedAt,
    string? BeforeJson,
    string? AfterJson);
