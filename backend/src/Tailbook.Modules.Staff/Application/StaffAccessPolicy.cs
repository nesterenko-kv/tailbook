using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Staff.Application;

public sealed class StaffAccessPolicy : IStaffAccessPolicy
{
    private const string StaffReadPermission = "staff.read";
    private const string StaffWritePermission = "staff.write";

    public bool CanReadStaff(ICurrentUser currentUser) => currentUser.HasPermission(StaffReadPermission);
    public bool CanWriteStaff(ICurrentUser currentUser) => currentUser.HasPermission(StaffWritePermission);
}
