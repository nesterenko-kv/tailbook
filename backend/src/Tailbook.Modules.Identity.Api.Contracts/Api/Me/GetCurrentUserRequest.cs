using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Identity.Api.Me;

public sealed class GetCurrentUserRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }

    [FromClaim(TailbookClaimTypes.ClientId, IsRequired = false)]
    public Guid? ClientId { get; set; }

    [FromClaim(TailbookClaimTypes.ContactPersonId, IsRequired = false)]
    public Guid? ContactPersonId { get; set; }
}
