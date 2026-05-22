namespace Tailbook.Modules.Notifications.Application.Abstractions.Services;

public sealed record NotificationDispatchEnvelope(Guid JobId, string Channel, string Recipient, string Subject, string Body, DateTimeOffset HappenedAt);