namespace Tailbook.Modules.Audit.Api.Admin.ListAuditEntries;

public sealed class AuditEntryItemResponse
{
    public Guid Id { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public DateTimeOffset HappenedAt { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}