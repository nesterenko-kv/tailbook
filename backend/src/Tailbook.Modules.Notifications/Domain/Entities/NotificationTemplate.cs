namespace Tailbook.Modules.Notifications.Domain.Entities;

public sealed class NotificationTemplate
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
