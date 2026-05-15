namespace Tailbook.Modules.Notifications.Application.Notifications.Models;

public sealed record NotificationDeliveryAttemptView(
    Guid Id,
    int AttemptNo,
    string Status,
    string? ErrorMessage,
    DateTimeOffset AttemptedAt);