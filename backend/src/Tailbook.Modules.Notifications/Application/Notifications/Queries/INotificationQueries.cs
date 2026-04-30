namespace Tailbook.Modules.Notifications.Application.Notifications.Queries;

public interface INotificationQueries
{
    Task<int> ProcessOutboxAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<NotificationJobListItemView>> ListJobsAsync(string? status, CancellationToken cancellationToken);
}
