namespace Tailbook.Modules.Audit.Api.Admin.ListAuditEntries;

public sealed class ListAuditEntriesRequest
{
    public string? ModuleCode { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
