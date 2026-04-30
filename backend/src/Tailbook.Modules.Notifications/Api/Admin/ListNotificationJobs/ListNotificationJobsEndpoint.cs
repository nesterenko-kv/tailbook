using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Notifications.Api.Admin.ListNotificationJobs;

public sealed class ListNotificationJobsEndpoint(NotificationQueries notificationQueries)
    : Endpoint<ListNotificationJobsRequest, ListNotificationJobsResponse>
{
    public override void Configure()
    {
        Get("/api/admin/notifications/jobs");
        Description(x => x.WithTags("Notifications"));
        PermissionsAll("notifications.read");
    }

    public override async Task HandleAsync(ListNotificationJobsRequest req, CancellationToken ct)
    {
        var items = await notificationQueries.ListJobsAsync(req.Status, ct);
        await Send.OkAsync(new ListNotificationJobsResponse { Items = items }, ct);
    }
}

public sealed class ListNotificationJobsRequest
{
    public string? Status { get; set; }
}

public sealed class ListNotificationJobsResponse
{
    public IReadOnlyCollection<NotificationJobListItemView> Items { get; set; } = [];
}
