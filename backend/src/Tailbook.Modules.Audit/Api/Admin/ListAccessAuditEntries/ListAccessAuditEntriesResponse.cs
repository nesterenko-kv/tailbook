namespace Tailbook.Modules.Audit.Api.Admin.ListAccessAuditEntries;

public sealed class ListAccessAuditEntriesResponse
{
    public IReadOnlyCollection<AccessAuditItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class AccessAuditItemResponse
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public DateTime HappenedAtUtc { get; set; }
}
