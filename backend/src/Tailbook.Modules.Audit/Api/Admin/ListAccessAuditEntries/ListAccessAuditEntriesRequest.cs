namespace Tailbook.Modules.Audit.Api.Admin.ListAccessAuditEntries;

public sealed class ListAccessAuditEntriesRequest
{
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
