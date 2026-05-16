using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Notifications.Api.Admin.NotificationProviderHealth;

public sealed class NotificationProviderHealthEndpoint(INotificationReadService notificationReadService)
    : EndpointWithoutRequest<NotificationProviderHealthView>
{
    public override void Configure()
    {
        Get("/api/admin/notifications/provider/health");
        Description(x => x.WithTags("Notifications"));
        PermissionsAll("notifications.read");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var health = await notificationReadService.GetProviderHealthAsync(ct);
        await Send.OkAsync(health, ct);
    }
}
