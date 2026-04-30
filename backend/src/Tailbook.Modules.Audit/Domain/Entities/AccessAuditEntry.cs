namespace Tailbook.Modules.Audit.Domain.Entities;

public sealed class AccessAuditEntry
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public DateTime HappenedAtUtc { get; set; }
}
