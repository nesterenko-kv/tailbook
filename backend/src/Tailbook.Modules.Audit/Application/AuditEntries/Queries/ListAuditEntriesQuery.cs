namespace Tailbook.Modules.Audit.Application.AuditEntries.Queries;

public sealed record ListAuditEntriesQuery(
    string? ModuleCode,
    string? EntityType,
    string? EntityId,
    int Page,
    int PageSize);
