using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Application;

public sealed class BookingAccessPolicy : IBookingAccessPolicy
{
    private const string BookingReadPermission = "booking.read";
    private const string BookingWritePermission = "booking.write";
    private const string CatalogReadPermission = "catalog.read";
    private const string PetsReadPermission = "pets.read";

    public bool CanPreviewQuotes(ICurrentUser currentUser)
    {
        return currentUser.HasPermission(CatalogReadPermission)
               && currentUser.HasPermission(PetsReadPermission)
               && currentUser.HasPermission(BookingReadPermission);
    }

    public bool CanReadBooking(ICurrentUser currentUser)
    {
        return currentUser.HasPermission(BookingReadPermission);
    }

    public bool CanWriteBooking(ICurrentUser currentUser)
    {
        return currentUser.HasPermission(BookingWritePermission);
    }
}
