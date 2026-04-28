using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Application;

public sealed class BookingAccessPolicy : IBookingAccessPolicy
{
    private const string BookingWritePermission = "booking.write";
    private const string BookingReadPermission = "booking.read";

    public bool CanReadBooking(ICurrentUser currentUser)
    {
        return currentUser.HasPermission(BookingReadPermission);
    }

    public bool CanWriteBooking(ICurrentUser currentUser)
    {
        return currentUser.HasPermission(BookingWritePermission);
    }
}
