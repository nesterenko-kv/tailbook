using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Identity.Api.Admin.AssignRoles;

public sealed class AssignRolesRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid Id { get; set; }
    public IReadOnlyCollection<string> RoleCodes { get; set; } = [];
}
