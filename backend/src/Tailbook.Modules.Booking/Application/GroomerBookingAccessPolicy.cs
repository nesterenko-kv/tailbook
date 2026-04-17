using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Identity.Contracts;

namespace Tailbook.Modules.Booking.Application;

public sealed class GroomerBookingAccessPolicy : IGroomerBookingAccessPolicy
{
    public bool CanReadAssignedAppointments(ICurrentUser currentUser)
    {
        return currentUser.HasPermission(PermissionCodes.GroomerAppAccess)
               && currentUser.HasPermission(PermissionCodes.GroomerAppointmentsRead);
    }
}
