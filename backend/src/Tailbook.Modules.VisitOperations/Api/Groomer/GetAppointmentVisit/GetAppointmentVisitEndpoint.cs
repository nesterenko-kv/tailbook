using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.VisitOperations.Application;

namespace Tailbook.Modules.VisitOperations.Api.Groomer.GetAppointmentVisit;

public sealed class GetAppointmentVisitEndpoint(GroomerVisitQueries groomerVisitQueries)
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
        try
        {
            var result = await groomerVisitQueries.GetVisitByAppointmentAsync(req.UserId, req.AppointmentId, ct);
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

public sealed class GetAppointmentVisitRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }

    public Guid AppointmentId { get; set; }
}
