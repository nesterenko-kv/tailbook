using ErrorOr;

namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IIdentityQueries
{
    Task<IReadOnlyList<RoleView>> ListRolesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PermissionView>> ListPermissionsAsync(CancellationToken cancellationToken);
    Task<PagedResult<UserSummaryView>> ListUsersAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<UserDetailView?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<ErrorOr<UserDetailView>> CreateUserAsync(string email, string displayName, string password, IReadOnlyCollection<string> roleCodes, Guid? assignedByUserId, CancellationToken cancellationToken);
    Task<ErrorOr<UserDetailView>> AssignRolesAsync(Guid userId, IReadOnlyCollection<string> roleCodes, Guid? assignedByUserId, CancellationToken cancellationToken);
}
