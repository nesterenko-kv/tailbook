namespace Tailbook.Modules.Notifications.Application.Notifications.Models;

public sealed record NotificationJobDetailView(
    Guid Id,
    string SourceEventType,
    string Channel,
    string Recipient,
    string Status,
    int AttemptCount,
    string? LastErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? NextAttemptAt,
    DateTimeOffset? DeadLetteredAt,
    IReadOnlyCollection<NotificationDeliveryAttemptView> Attempts);