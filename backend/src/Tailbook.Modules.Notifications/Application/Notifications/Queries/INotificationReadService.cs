namespace Tailbook.Modules.Notifications.Application.Notifications.Queries;

public interface INotificationReadService
{
    Task<IReadOnlyCollection<NotificationJobListItemView>> ListJobsAsync(string? status, CancellationToken cancellationToken);
}
