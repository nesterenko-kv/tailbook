namespace Tailbook.Modules.Notifications.Api.Admin.ListNotificationJobs;

public sealed class ListNotificationJobsResponse
{
    public IReadOnlyCollection<NotificationJobListItemView> Items { get; set; } = [];
}