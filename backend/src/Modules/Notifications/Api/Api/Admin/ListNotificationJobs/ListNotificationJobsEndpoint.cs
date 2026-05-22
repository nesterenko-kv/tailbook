using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Notifications.Api.Admin.ListNotificationJobs;

public sealed class ListNotificationJobsEndpoint(INotificationReadService notificationReadService)
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
        var query = new NotificationJobListQuery(req.Status, req.EventType, req.CreatedFrom, req.CreatedTo);
        var items = await notificationReadService.ListJobsAsync(query, ct);
        await Send.OkAsync(new ListNotificationJobsResponse { Items = items }, ct);
    }
}