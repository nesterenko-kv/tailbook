namespace Tailbook.Modules.Audit.Application.AccessAuditEntries.Queries;

public sealed record ListAccessAuditEntriesQuery(
    string? ResourceType,
    string? ResourceId,
    int Page,
    int PageSize);
