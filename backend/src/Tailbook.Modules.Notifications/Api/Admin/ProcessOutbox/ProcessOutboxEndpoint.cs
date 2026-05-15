using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Notifications.Api.Admin.ProcessOutbox;

public sealed class ProcessOutboxEndpoint : EndpointWithoutRequest<ProcessOutboxResponse>
{
    public override void Configure()
    {
        Post("/api/admin/notifications/outbox/process");
        Description(x => x.WithTags("Notifications"));
        PermissionsAll("notifications.write");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var command = new ProcessNotificationOutboxCommand();
        var processed = await command.ExecuteAsync(ct);
        await Send.OkAsync(new ProcessOutboxResponse { ProcessedCount = processed }, ct);
    }
}
