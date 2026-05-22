namespace Tailbook.Modules.Audit.Api.Admin.ListAuditEntries;

public sealed class ListAuditEntriesResponse
{
    public IReadOnlyCollection<AuditEntryItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}