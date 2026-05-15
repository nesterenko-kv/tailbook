using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Notifications.Api.Admin.RequeueNotificationJob;

public sealed class RequeueNotificationJobEndpoint : Endpoint<RequeueNotificationJobRequest, NotificationJobListItemView>
{
    public override void Configure()
    {
        Post("/api/admin/notifications/jobs/{jobId:guid}/requeue");
        Description(x => x.WithTags("Notifications"));
        PermissionsAll("notifications.write");
    }

    public override async Task HandleAsync(RequeueNotificationJobRequest req, CancellationToken ct)
    {
        var command = new RequeueNotificationJobCommand(req.JobId, req.ActorUserId);
        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(result.Value, cancellation: ct);
    }
}
