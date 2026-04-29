using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Contracts;
using Tailbook.Modules.Identity.Domain;

namespace Tailbook.Modules.Identity.Application;

public class AuthenticateUserCommandHandler(
    AppDbContext dbContext,
    PasswordHasher passwordHasher,
    IdentitySessionService identitySessionService
) : ICommandHandler<AuthenticateUserCommand, LoginResult?>
{
    public async Task<LoginResult?> ExecuteAsync(AuthenticateUserCommand command, CancellationToken cancellationToken)
    {
        var password = command.Password;
        var email = command.Email;

        var normalizedEmail = NormalizeEmail(email);
        var user = await dbContext.Set<IdentityUser>()
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null || !string.Equals(user.Status, UserStatusCodes.Active, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!passwordHasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        var roles = await GetRoleCodesAsync(user.Id, cancellationToken);
        var permissions = await GetPermissionCodesAsync(user.Id, cancellationToken);

        return await identitySessionService.CreateSessionAsync(user, roles, permissions, cancellationToken);
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
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken: cancellationToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
