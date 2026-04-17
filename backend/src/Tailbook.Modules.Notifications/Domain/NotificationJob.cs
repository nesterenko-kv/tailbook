namespace Tailbook.Modules.Notifications.Domain;

public sealed class NotificationJob
{
    public Guid Id { get; set; }
    public string SourceEventType { get; set; } = string.Empty;
    public Guid? SourceEventMessageId { get; set; }
    public Guid? TemplateId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
}
