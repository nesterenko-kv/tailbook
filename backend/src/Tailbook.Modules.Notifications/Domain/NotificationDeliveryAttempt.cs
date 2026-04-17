namespace Tailbook.Modules.Notifications.Domain;

public sealed class NotificationDeliveryAttempt
{
    public Guid Id { get; set; }
    public Guid NotificationJobId { get; set; }
    public int AttemptNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime AttemptedAtUtc { get; set; }
}
