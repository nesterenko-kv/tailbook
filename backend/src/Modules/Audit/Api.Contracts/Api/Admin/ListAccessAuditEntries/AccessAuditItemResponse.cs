namespace Tailbook.Modules.Audit.Api.Admin.ListAccessAuditEntries;

public sealed class AccessAuditItemResponse
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public DateTimeOffset HappenedAt { get; set; }
}