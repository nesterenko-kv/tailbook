namespace Tailbook.Modules.Notifications.Application;

public interface INotificationSink
{
    Task SendAsync(NotificationDispatchEnvelope envelope, CancellationToken cancellationToken);
}

public sealed record NotificationDispatchEnvelope(Guid JobId, string Channel, string Recipient, string Subject, string Body, DateTime HappenedAtUtc);
