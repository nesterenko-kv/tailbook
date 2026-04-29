using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Notifications.Application;

namespace Tailbook.Modules.Notifications.Api.Admin.ProcessOutbox;

public sealed class ProcessOutboxEndpoint(NotificationQueries notificationQueries)
    : EndpointWithoutRequest<ProcessOutboxResponse>
{
    public override void Configure()
    {
        Post("/api/admin/notifications/outbox/process");
        Description(x => x.WithTags("Notifications"));
        PermissionsAll("notifications.write");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var processed = await notificationQueries.ProcessOutboxAsync(ct);
        await Send.OkAsync(new ProcessOutboxResponse { ProcessedCount = processed }, ct);
    }
}

public sealed class ProcessOutboxResponse
{
    public int ProcessedCount { get; set; }
}
