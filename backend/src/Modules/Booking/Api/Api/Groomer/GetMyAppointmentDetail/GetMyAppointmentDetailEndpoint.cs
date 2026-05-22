using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Groomer.GetMyAppointmentDetail;

public sealed class GetMyAppointmentDetailEndpoint(
    IGroomerBookingReadService groomerBookingReadService)
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
        var result = await groomerBookingReadService.GetAssignedAppointmentAsync(req.UserId, req.AppointmentId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}
