using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.VisitOperations.Api.Admin.CompleteVisit;

public sealed class CompleteVisitRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid ActorUserId { get; set; }

    public Guid VisitId { get; set; }
}
