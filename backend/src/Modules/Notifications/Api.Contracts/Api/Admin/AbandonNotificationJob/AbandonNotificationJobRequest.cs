using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Notifications.Api.Admin.AbandonNotificationJob;

public sealed class AbandonNotificationJobRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid ActorUserId { get; set; }

    public Guid JobId { get; set; }
}
