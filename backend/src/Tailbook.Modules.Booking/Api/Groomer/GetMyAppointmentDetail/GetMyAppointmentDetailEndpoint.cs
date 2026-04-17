using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Groomer.GetMyAppointmentDetail;

public sealed class GetMyAppointmentDetailEndpoint(
    ICurrentUser currentUser,
    IGroomerBookingAccessPolicy accessPolicy,
    GroomerBookingQueries groomerBookingQueries)
    : Endpoint<GetMyAppointmentDetailRequest, GroomerAppointmentDetailView>
{
    public override void Configure()
    {
        Get("/api/groomer/appointments/{appointmentId:guid}");
        Description(x => x.WithTags("Groomer Booking"));
    }

    public override async Task HandleAsync(GetMyAppointmentDetailRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanReadAssignedAppointments(currentUser))
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
            var result = await groomerBookingQueries.GetAssignedAppointmentAsync(currentUserId, req.AppointmentId, ct);
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

public sealed class GetMyAppointmentDetailRequest
{
    public Guid AppointmentId { get; set; }
}
