namespace Tailbook.Modules.Audit.Infrastructure.WriteBuffering;

internal abstract record AuditWriteItem(Guid Id, Guid? ActorUserId, DateTimeOffset HappenedAt);

internal sealed record AccessAuditWriteItem(
    Guid Id,
    Guid? ActorUserId,
    string ResourceType,
    string ResourceId,
    string ActionCode,
    DateTimeOffset HappenedAt) : AuditWriteItem(Id, ActorUserId, HappenedAt);

internal sealed record AuditTrailWriteItem(
    Guid Id,
    Guid? ActorUserId,
    string ModuleCode,
    string EntityType,
    string EntityId,
    string ActionCode,
    DateTimeOffset HappenedAt,
    string? BeforeJson,
    string? AfterJson) : AuditWriteItem(Id, ActorUserId, HappenedAt);

internal static class AuditWriteItemTypes
{
    public static string GetTelemetryItemType(AuditWriteItem item)
    {
        return item switch
        {
            AccessAuditWriteItem => Telemetry.AuditTelemetry.ItemTypeAccessAudit,
            AuditTrailWriteItem => Telemetry.AuditTelemetry.ItemTypeAuditTrail,
            _ => "unknown"
        };
    }
}
