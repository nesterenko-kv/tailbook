using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Admin.GetAppointmentDetail;

public sealed class GetAppointmentDetailEndpoint(ICurrentUser currentUser, IBookingAccessPolicy accessPolicy, BookingManagementQueries bookingQueries)
    : Endpoint<GetAppointmentDetailRequest, AppointmentDetailView>
{
    public override void Configure()
    {
        Get("/api/admin/appointments/{appointmentId:guid}");
        Description(x => x.WithTags("Admin Booking"));
    }

    public override async Task HandleAsync(GetAppointmentDetailRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanReadBooking(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

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
