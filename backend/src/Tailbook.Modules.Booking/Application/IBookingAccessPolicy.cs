using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Application;

public interface IBookingAccessPolicy
{
    bool CanPreviewQuotes(ICurrentUser currentUser);
}
