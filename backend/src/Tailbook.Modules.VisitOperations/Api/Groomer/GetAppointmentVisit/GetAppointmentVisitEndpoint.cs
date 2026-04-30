using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Groomer.GetAppointmentVisit;

public sealed class GetAppointmentVisitEndpoint(IGroomerVisitQueries groomerVisitQueries)
    : Endpoint<GetAppointmentVisitRequest, GroomerVisitDetailView>
{
    public override void Configure()
    {
        Get("/api/groomer/appointments/{appointmentId:guid}/visit");
        Description(x => x.WithTags("Groomer Visits"));
        PermissionsAll("app.groomer.access", "groomer.visits.read");
    }

    public override async Task HandleAsync(GetAppointmentVisitRequest req, CancellationToken ct)
    {
        var result = await groomerVisitQueries.GetVisitByAppointmentAsync(req.UserId, req.AppointmentId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class GetAppointmentVisitRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }

    public Guid AppointmentId { get; set; }
}
