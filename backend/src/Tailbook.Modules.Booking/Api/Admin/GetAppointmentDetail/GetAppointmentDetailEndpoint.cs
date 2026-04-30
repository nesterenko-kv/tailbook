using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Booking.Api.Admin.GetAppointmentDetail;

public sealed class GetAppointmentDetailEndpoint(BookingManagementQueries bookingQueries)
    : Endpoint<GetAppointmentDetailRequest, AppointmentDetailView>
{
    public override void Configure()
    {
        Get("/api/admin/appointments/{appointmentId:guid}");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.read");
    }

    public override async Task HandleAsync(GetAppointmentDetailRequest req, CancellationToken ct)
    {
        var result = await bookingQueries.GetAppointmentAsync(req.AppointmentId, ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(result, cancellation: ct);
    }
}

public sealed class GetAppointmentDetailRequest
{
    public Guid AppointmentId { get; set; }
}
