namespace Tailbook.Modules.Notifications.Api.Admin.ListNotificationJobs;

public sealed class ListNotificationJobsRequest
{
    public string? Status { get; set; }
    public string? EventType { get; set; }
    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedTo { get; set; }
}