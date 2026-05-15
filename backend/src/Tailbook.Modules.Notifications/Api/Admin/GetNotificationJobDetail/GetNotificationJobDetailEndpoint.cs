using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Notifications.Api.Admin.GetNotificationJobDetail;

public sealed class GetNotificationJobDetailEndpoint(
    INotificationReadService notificationReadService,
    IEntityScopeService entityScopeService)
    : Endpoint<GetNotificationJobDetailRequest, NotificationJobDetailView>
{
    public override void Configure()
    {
        Get("/api/admin/notifications/jobs/{jobId:guid}");
        Description(x => x.WithTags("Notifications"));
        PermissionsAll("notifications.read");
    }

    public override async Task HandleAsync(GetNotificationJobDetailRequest req, CancellationToken ct)
    {
        var job = await notificationReadService.GetJobAsync(req.JobId, ct);
        if (job is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var scopeResult = await entityScopeService.VerifyAccessAsync(EntityScopeResourceTypes.NotificationJob, req.JobId.ToString("D"), req.ActorUserId, ct);
        if (scopeResult.IsError)
        {
            await Send.ResultAsync(scopeResult.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(job, cancellation: ct);
    }
}