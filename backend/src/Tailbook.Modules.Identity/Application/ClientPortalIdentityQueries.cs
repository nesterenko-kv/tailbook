using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Contracts;
using Tailbook.Modules.Identity.Domain;

namespace Tailbook.Modules.Identity.Application;

public sealed class ClientPortalIdentityQueries(
    AppDbContext dbContext,
    PasswordHasher passwordHasher,
    IdentitySessionService identitySessionService)
{
    public async Task<LoginResult?> AuthenticateClientAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = IdentityQueries.NormalizeEmail(email);
        var user = await dbContext.Set<IdentityUser>()
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null || user.ClientId is null || user.ContactPersonId is null || !string.Equals(user.Status, UserStatusCodes.Active, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!passwordHasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        var roles = await dbContext.Set<UserRoleAssignment>()
            .Where(x => x.UserId == user.Id)
            .Join(dbContext.Set<IdentityRole>(), x => x.RoleId, x => x.Id, (_, role) => role.Code)
            .Distinct()
            .OrderBy(x => x)
            .ToArrayAsync(cancellationToken);

        var permissions = await dbContext.Set<UserRoleAssignment>()
            .Where(x => x.UserId == user.Id)
            .Join(dbContext.Set<RolePermission>(), assignment => assignment.RoleId, rolePermission => rolePermission.RoleId, (_, rolePermission) => rolePermission.PermissionId)
            .Join(dbContext.Set<IdentityPermission>(), permissionId => permissionId, permission => permission.Id, (_, permission) => permission.Code)
            .Distinct()
            .OrderBy(x => x)
            .ToArrayAsync(cancellationToken);

        if (!permissions.Contains(PermissionCodes.ClientPortalAccess, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        return await identitySessionService.CreateSessionAsync(user, roles, permissions, cancellationToken);
    }

    public async Task<ClientPortalActor?> GetActorAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<IdentityUser>().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null || user.ClientId is null || user.ContactPersonId is null)
        {
            return null;
        }

        return new ClientPortalActor(user.Id, user.ClientId.Value, user.ContactPersonId.Value, user.Email, user.DisplayName);
    }
}
