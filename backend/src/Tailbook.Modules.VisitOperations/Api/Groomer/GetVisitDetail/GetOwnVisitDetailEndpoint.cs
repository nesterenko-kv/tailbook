using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.VisitOperations.Application;

namespace Tailbook.Modules.VisitOperations.Api.Groomer.GetVisitDetail;

public sealed class GetOwnVisitDetailEndpoint(
    ICurrentUser currentUser,
    IGroomerVisitAccessPolicy accessPolicy,
    GroomerVisitQueries groomerVisitQueries)
    : Endpoint<GetOwnVisitDetailRequest, GroomerVisitDetailView>
{
    public override void Configure()
    {
        Get("/api/groomer/visits/{visitId:guid}");
        Description(x => x.WithTags("Groomer Visits"));
    }

    public override async Task HandleAsync(GetOwnVisitDetailRequest req, CancellationToken ct)
    {
        if (!accessPolicy.CanReadOwnVisits(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (!Guid.TryParse(currentUser.UserId, out var currentUserId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var result = await groomerVisitQueries.GetVisitAsync(currentUserId, req.VisitId, ct);
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
    public Guid VisitId { get; set; }
}
