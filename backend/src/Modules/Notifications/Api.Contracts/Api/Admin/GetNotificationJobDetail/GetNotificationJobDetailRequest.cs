using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Notifications.Api.Admin.GetNotificationJobDetail;

public sealed class GetNotificationJobDetailRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid ActorUserId { get; set; }

    public Guid JobId { get; set; }
}
