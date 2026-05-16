using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Notifications.Api.Admin.NotificationDashboard;

public sealed class NotificationDashboardEndpoint(INotificationReadService notificationReadService)
    : EndpointWithoutRequest<NotificationDashboardView>
{
    public override void Configure()
    {
        Get("/api/admin/notifications/dashboard");
        Description(x => x.WithTags("Notifications"));
        PermissionsAll("notifications.read");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var dashboard = await notificationReadService.GetDashboardAsync(ct);
        await Send.OkAsync(dashboard, ct);
    }
}
