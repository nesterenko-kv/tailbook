namespace Tailbook.Modules.Audit.Api.Admin.ListAuditEntries;

public sealed class ListAuditEntriesResponse
{
    public IReadOnlyCollection<AuditEntryItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class AuditEntryItemResponse
{
    public Guid Id { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public DateTime HappenedAtUtc { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}
