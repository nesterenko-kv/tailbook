using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Notifications.Application;

namespace Tailbook.Modules.Notifications.Api.Admin.ListNotificationJobs;

public sealed class ListNotificationJobsEndpoint(ICurrentUser currentUser, NotificationQueries notificationQueries)
    : Endpoint<ListNotificationJobsRequest, ListNotificationJobsResponse>
{
    private const string NotificationReadPermission = "notifications.read";

    public override void Configure()
    {
        Get("/api/admin/notifications/jobs");
        Description(x => x.WithTags("Notifications"));
    }

    public override async Task HandleAsync(ListNotificationJobsRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        if (!currentUser.HasPermission(NotificationReadPermission))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

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
