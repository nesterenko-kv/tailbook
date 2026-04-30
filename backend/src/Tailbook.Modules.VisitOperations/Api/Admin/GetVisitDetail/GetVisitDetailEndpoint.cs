using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.VisitOperations.Api.Admin.GetVisitDetail;

public sealed class GetVisitDetailEndpoint(VisitQueries visitQueries)
    : Endpoint<GetVisitDetailRequest, VisitDetailView>
{
    public override void Configure()
    {
        Get("/api/admin/visits/{visitId:guid}");
        Description(x => x.WithTags("Admin Visits"));
        PermissionsAll("visit.read");
    }

    public override async Task HandleAsync(GetVisitDetailRequest req, CancellationToken ct)
    {
        var result = await visitQueries.GetVisitAsync(req.VisitId, req.ActorUserId, ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(result, ct);
    }
}

public sealed class GetVisitDetailRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid VisitId { get; set; }
}
