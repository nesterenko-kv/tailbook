namespace Tailbook.Modules.Notifications.Application.Notifications.Queries;

public interface INotificationReadService
{
    Task<IReadOnlyCollection<NotificationJobListItemView>> ListJobsAsync(NotificationJobListQuery query, CancellationToken cancellationToken);
    Task<NotificationJobDetailView?> GetJobAsync(Guid jobId, CancellationToken cancellationToken);
    Task<NotificationDashboardView> GetDashboardAsync(CancellationToken cancellationToken);
    Task<NotificationProviderHealthView> GetProviderHealthAsync(CancellationToken cancellationToken);
}

public sealed record NotificationStatusCount(string Status, int Count);

public sealed record NotificationDeadLetterByEvent(string EventType, int Count);

public sealed record NotificationDashboardView(
    IReadOnlyCollection<NotificationStatusCount> StatusCounts,
    IReadOnlyCollection<NotificationDeadLetterByEvent> DeadLetterByEventType,
    int DeadLetterToday,
    int DeadLetterLast7Days,
    int DeadLetterOlder,
    int TotalDeliveryAttempts,
    int SuccessfulDeliveries,
    double SuccessRate);

public sealed record NotificationProviderHealthView(
    string ProviderType,
    bool IsConfigured,
    string LastDeliveryStatus,
    int LastDeliveryAttempts,
    int LastDeliveryFailures,
    string? LastErrorMessage);

