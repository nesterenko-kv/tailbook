using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Application;

public interface IBookingAccessPolicy
{
    bool CanReadBooking(ICurrentUser currentUser);
    bool CanWriteBooking(ICurrentUser currentUser);
}
