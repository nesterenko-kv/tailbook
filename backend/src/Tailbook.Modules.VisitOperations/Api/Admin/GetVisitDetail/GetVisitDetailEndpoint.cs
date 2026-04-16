using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.VisitOperations.Application;

namespace Tailbook.Modules.VisitOperations.Api.Admin.GetVisitDetail;

public sealed class GetVisitDetailEndpoint(ICurrentUser currentUser, IVisitOperationsAccessPolicy accessPolicy, VisitQueries visitQueries)
    : Endpoint<GetVisitDetailRequest, VisitDetailView>
{
    public override void Configure()
    {
        Get("/api/admin/visits/{visitId:guid}");
        Description(x => x.WithTags("Admin Visits"));
    }

    public override async Task HandleAsync(GetVisitDetailRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanReadVisits(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var actorUserId = Guid.TryParse(currentUser.UserId, out var parsed) ? parsed : (Guid?)null;
        var result = await visitQueries.GetVisitAsync(req.VisitId, actorUserId, ct);
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
    public Guid VisitId { get; set; }
}
