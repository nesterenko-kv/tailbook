using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Identity.Api.Admin.GetUserById;

public sealed class GetUserByIdRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid Id { get; set; }
}
