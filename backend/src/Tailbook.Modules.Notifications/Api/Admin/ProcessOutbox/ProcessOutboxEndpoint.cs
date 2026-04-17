using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Notifications.Application;

namespace Tailbook.Modules.Notifications.Api.Admin.ProcessOutbox;

public sealed class ProcessOutboxEndpoint(ICurrentUser currentUser, NotificationQueries notificationQueries)
    : EndpointWithoutRequest<ProcessOutboxResponse>
{
    private const string NotificationWritePermission = "notifications.write";

    public override void Configure()
    {
        Post("/api/admin/notifications/outbox/process");
        Description(x => x.WithTags("Notifications"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        if (!currentUser.HasPermission(NotificationWritePermission))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var processed = await notificationQueries.ProcessOutboxAsync(ct);
        await Send.OkAsync(new ProcessOutboxResponse { ProcessedCount = processed }, ct);
    }
}

public sealed class ProcessOutboxResponse
{
    public int ProcessedCount { get; set; }
}
