using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.VisitOperations.Application;

namespace Tailbook.Modules.VisitOperations.Api.Groomer.GetVisitDetail;

public sealed class GetOwnVisitDetailEndpoint(GroomerVisitQueries groomerVisitQueries)
    : Endpoint<GetOwnVisitDetailRequest, GroomerVisitDetailView>
{
    public override void Configure()
    {
        Get("/api/groomer/visits/{visitId:guid}");
        Description(x => x.WithTags("Groomer Visits"));
        PermissionsAll("app.groomer.access", "groomer.visits.read");
    }

    public override async Task HandleAsync(GetOwnVisitDetailRequest req, CancellationToken ct)
    {
        try
        {
            var result = await groomerVisitQueries.GetVisitAsync(req.UserId, req.VisitId, ct);
            if (result is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.OkAsync(result, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}

public sealed class GetOwnVisitDetailRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }

    public Guid VisitId { get; set; }
}
