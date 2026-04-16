using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Staff.Application;

public interface IStaffAccessPolicy
{
    bool CanReadStaff(ICurrentUser currentUser);
    bool CanWriteStaff(ICurrentUser currentUser);
}
