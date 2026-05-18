using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Notifications.Api.Admin.ProcessNotifications;

public sealed class ProcessNotificationsEndpoint : EndpointWithoutRequest<ProcessNotificationsResponse>
{
    public override void Configure()
    {
        Post("/api/admin/notifications/process");
        Description(x => x.WithTags("Notifications"));
        PermissionsAll("notifications.write");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var command = new ProcessNotificationsCommand();
        var processed = await command.ExecuteAsync(ct);
        await Send.OkAsync(new ProcessNotificationsResponse { ProcessedCount = processed }, ct);
    }
}
