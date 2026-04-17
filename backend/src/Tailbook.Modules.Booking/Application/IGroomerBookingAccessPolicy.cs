using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Application;

public interface IGroomerBookingAccessPolicy
{
    bool CanReadAssignedAppointments(ICurrentUser currentUser);
}
