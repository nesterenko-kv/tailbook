using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed class IdentitySessionService(
    AppDbContext dbContext,
    JwtTokenFactory jwtTokenFactory,
    RefreshTokenService refreshTokenService) : IIdentitySessionService
{
    public async Task<LoginResult> CreateSessionAsync(
        IdentityUser user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken)
    {
        var accessToken = jwtTokenFactory.CreateToken(user.Id.ToString("D"), user.SubjectId, user.Email, user.DisplayName, user.ClientId, user.ContactPersonId, roles, permissions);
        var refreshToken = await refreshTokenService.IssueAsync(user.Id, cancellationToken);

        return new LoginResult(
            accessToken.AccessToken,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            new AuthenticatedUserView(user.Id, user.SubjectId, user.Email, user.DisplayName, user.Status, user.ClientId, user.ContactPersonId, roles, permissions));
    }

    public async Task<ErrorOr<LoginResult>> CreateSessionAsync(Guid userId, bool requireClientPortalAccess, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<IdentityUser>()
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null || !string.Equals(user.Status, UserStatusCodes.Active, StringComparison.OrdinalIgnoreCase))
        {
            return IdentityErrors.InvalidCredentials();
        }

        if (requireClientPortalAccess && (user.ClientId is null || user.ContactPersonId is null))
        {
            return IdentityErrors.ClientPortalAccessRequired();
        }

        var roles = await GetRoleCodesAsync(user.Id, cancellationToken);
        var permissions = await GetPermissionCodesAsync(user.Id, cancellationToken);

        if (requireClientPortalAccess && !permissions.Contains(PermissionCodes.ClientPortalAccess, StringComparer.OrdinalIgnoreCase))
        {
            return IdentityErrors.ClientPortalAccessRequired();
        }

        return await CreateSessionAsync(user, roles, permissions, cancellationToken);
    }

    public async Task<ErrorOr<LoginResult>> RefreshSessionAsync(string refreshToken, bool requireClientPortalAccess, CancellationToken cancellationToken)
    {
        var storedRefreshToken = await refreshTokenService.FindUsableAsync(refreshToken, cancellationToken);
        if (storedRefreshToken is null)
        {
            return IdentityErrors.InvalidRefreshToken();
        }

        var user = await dbContext.Set<IdentityUser>()
            .SingleOrDefaultAsync(x => x.Id == storedRefreshToken.UserId, cancellationToken);
        if (user is null || !string.Equals(user.Status, UserStatusCodes.Active, StringComparison.OrdinalIgnoreCase))
        {
            return IdentityErrors.InvalidRefreshToken();
        }

        if (requireClientPortalAccess && (user.ClientId is null || user.ContactPersonId is null))
        {
            return IdentityErrors.ClientPortalAccessRequired();
        }

        var roles = await GetRoleCodesAsync(user.Id, cancellationToken);
        var permissions = await GetPermissionCodesAsync(user.Id, cancellationToken);

        if (requireClientPortalAccess && !permissions.Contains(PermissionCodes.ClientPortalAccess, StringComparer.OrdinalIgnoreCase))
        {
            return IdentityErrors.ClientPortalAccessRequired();
        }

        var accessToken = jwtTokenFactory.CreateToken(user.Id.ToString("D"), user.SubjectId, user.Email, user.DisplayName, user.ClientId, user.ContactPersonId, roles, permissions);
        var rotatedRefreshToken = await refreshTokenService.RotateAsync(storedRefreshToken, cancellationToken);

        return new LoginResult(
            accessToken.AccessToken,
            accessToken.ExpiresAt,
            rotatedRefreshToken.Token,
            rotatedRefreshToken.ExpiresAt,
            new AuthenticatedUserView(user.Id, user.SubjectId, user.Email, user.DisplayName, user.Status, user.ClientId, user.ContactPersonId, roles, permissions));
    }

    public async Task<ErrorOr<Success>> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var revoked = await refreshTokenService.RevokeAsync(refreshToken, cancellationToken);
        return revoked
            ? Result.Success
            : IdentityErrors.InvalidRefreshToken();
    }

    private async Task<HashSet<string>> GetRoleCodesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserRoleAssignment>()
            .Where(x => x.UserId == userId)
            .Join(
                dbContext.Set<IdentityRole>(),
                assignment => assignment.RoleId,
                role => role.Id,
                (_, role) => role.Code)
            .Distinct()
            .OrderBy(x => x)
            .AsAsyncEnumerable()
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    private async Task<HashSet<string>> GetPermissionCodesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserRoleAssignment>()
            .Where(x => x.UserId == userId)
            .Join(
                dbContext.Set<RolePermission>(),
                assignment => assignment.RoleId,
                rolePermission => rolePermission.RoleId,
                (_, rolePermission) => rolePermission.PermissionId)
            .Join(
                dbContext.Set<IdentityPermission>(),
                permissionId => permissionId,
                permission => permission.Id,
                (_, permission) => permission.Code)
            .Distinct()
            .OrderBy(x => x)
            .AsAsyncEnumerable()
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);
    }
}
