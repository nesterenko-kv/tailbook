using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Booking.Api.Groomer.GetMyAppointmentDetail;

public sealed class GetMyAppointmentDetailEndpoint(
    IGroomerBookingQueries groomerBookingQueries)
    : Endpoint<GetMyAppointmentDetailRequest, GroomerAppointmentDetailView>
{
    public override void Configure()
    {
        Get("/api/groomer/appointments/{appointmentId:guid}");
        Description(x => x.WithTags("Groomer Booking"));
        PermissionsAll("app.groomer.access", "groomer.appointments.read");
    }

    public override async Task HandleAsync(GetMyAppointmentDetailRequest req, CancellationToken ct)
    {
        try
        {
            var result = await groomerBookingQueries.GetAssignedAppointmentAsync(req.UserId, req.AppointmentId, ct);
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
