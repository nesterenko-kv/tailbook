namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IIdentityReadService
{
    Task<IReadOnlyList<RoleView>> ListRolesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PermissionView>> ListPermissionsAsync(CancellationToken cancellationToken);
    Task<PagedResult<UserSummaryView>> ListUsersAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<UserDetailView?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
}
