using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Application;

public sealed class BookingAccessPolicy : IBookingAccessPolicy
{
    private const string CatalogReadPermission = "catalog.read";
    private const string PetsReadPermission = "pets.read";

    public bool CanPreviewQuotes(ICurrentUser currentUser)
    {
        return currentUser.HasPermission(CatalogReadPermission)
               && currentUser.HasPermission(PetsReadPermission);
    }
}
