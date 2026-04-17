namespace Tailbook.Modules.Audit.Domain;

public sealed class AuditEntry
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
