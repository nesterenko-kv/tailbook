namespace Tailbook.Modules.Notifications.Application.Abstractions.Services;

public interface INotificationSink
{
    Task SendAsync(NotificationDispatchEnvelope envelope, CancellationToken cancellationToken);
}