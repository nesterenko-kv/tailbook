using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.VisitOperations.Application;

namespace Tailbook.Modules.VisitOperations.Api.Groomer.GetAppointmentVisit;

public sealed class GetAppointmentVisitEndpoint(
    ICurrentUser currentUser,
    IGroomerVisitAccessPolicy accessPolicy,
    GroomerVisitQueries groomerVisitQueries)
    : Endpoint<GetAppointmentVisitRequest, GroomerVisitDetailView>
{
    public override void Configure()
    {
        Get("/api/groomer/appointments/{appointmentId:guid}/visit");
        Description(x => x.WithTags("Groomer Visits"));
    }

    public override async Task HandleAsync(GetAppointmentVisitRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

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
            var result = await groomerVisitQueries.GetVisitByAppointmentAsync(currentUserId, req.AppointmentId, ct);
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
    public Guid AppointmentId { get; set; }
}
