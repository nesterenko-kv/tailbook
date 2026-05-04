namespace Tailbook.Modules.Notifications.Application.Notifications.Models;

public sealed record NotificationJobListItemView(Guid Id, string SourceEventType, string Channel, string Recipient, string Status, int AttemptCount, string? LastErrorMessage, DateTime CreatedAtUtc, DateTime? SentAtUtc);
