using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Admin.GetBookingRequestDetail;

public sealed class GetBookingRequestDetailEndpoint(
    ICurrentUser currentUser,
    IBookingAccessPolicy accessPolicy,
    BookingManagementQueries bookingQueries)
    : EndpointWithoutRequest<BookingRequestDetailView>
{
    public override void Configure()
    {
        Get("/api/admin/booking-requests/{bookingRequestId:guid}");
        Description(x => x.WithTags("Admin Booking"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!accessPolicy.CanReadBooking(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var bookingRequestId = Route<Guid>("bookingRequestId");
        var result = await bookingQueries.GetBookingRequestAsync(bookingRequestId, ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(result, ct);
    }
}
