using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Staff.Api.Admin.GetGroomerById;

public sealed class GetGroomerByIdRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid ActorUserId { get; set; }

    public Guid GroomerId { get; set; }
}