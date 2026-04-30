using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Booking.Api.Admin.GetBookingRequestDetail;

public sealed class GetBookingRequestDetailEndpoint(
    BookingManagementQueries bookingQueries)
    : Endpoint<GetBookingRequestDetailRequest, BookingRequestDetailView>
{
    public override void Configure()
    {
        Get("/api/admin/booking-requests/{bookingRequestId:guid}");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.read");
    }

    public override async Task HandleAsync(GetBookingRequestDetailRequest req, CancellationToken ct)
    {
        var result = await bookingQueries.GetBookingRequestAsync(req.BookingRequestId, ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(result, ct);
    }
}

public sealed class GetBookingRequestDetailRequest
{
    public Guid BookingRequestId { get; set; }
}
