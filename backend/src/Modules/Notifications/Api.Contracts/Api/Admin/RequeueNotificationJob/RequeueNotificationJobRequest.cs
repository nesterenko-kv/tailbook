using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Notifications.Api.Admin.RequeueNotificationJob;

public sealed class RequeueNotificationJobRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid ActorUserId { get; set; }

    public Guid JobId { get; set; }
}
