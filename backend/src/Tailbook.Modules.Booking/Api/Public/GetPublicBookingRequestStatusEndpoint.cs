using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Booking.Api.Public;

public sealed class GetPublicBookingRequestStatusEndpoint(
    PublicBookingReadService publicBookingReadService,
    IBookingManagementReadService bookingManagementReadService)
    : Endpoint<GetPublicBookingRequestStatusRequest, GetPublicBookingRequestStatusResponse>
{
    public override void Configure()
    {
        Get("/api/public/booking-requests/{bookingRequestId:guid}");
        AllowAnonymous();
        Description(x => x.WithTags("Public Booking"));
    }

    public override async Task HandleAsync(GetPublicBookingRequestStatusRequest req, CancellationToken ct)
    {
        var detail = await publicBookingReadService.GetBookingRequestStatusAsync(req.BookingRequestId, ct);
        if (detail is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var appointmentId = await bookingManagementReadService.GetAppointmentIdByBookingRequestAsync(req.BookingRequestId, ct);

        await Send.OkAsync(new GetPublicBookingRequestStatusResponse
        {
            Id = detail.Id,
            Status = detail.Status,
            CreatedAt = detail.CreatedAt,
            UpdatedAt = detail.UpdatedAt,
            AppointmentId = appointmentId
        }, ct);
    }
}
