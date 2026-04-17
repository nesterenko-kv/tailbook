using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Contracts;
using Tailbook.Modules.Identity.Domain;

namespace Tailbook.Modules.Identity.Application;

public sealed class ClientPortalIdentityQueries(
    AppDbContext dbContext,
    PasswordHasher passwordHasher,
    JwtTokenFactory jwtTokenFactory,
    IClientOnboardingService clientOnboardingService)
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

        var token = jwtTokenFactory.CreateToken(user.Id.ToString("D"), user.SubjectId, user.Email, user.DisplayName, roles, permissions);
        return new LoginResult(
            token.AccessToken,
            token.ExpiresAtUtc,
            new AuthenticatedUserView(user.Id, user.SubjectId, user.Email, user.DisplayName, user.Status, user.ClientId, user.ContactPersonId, roles, permissions));
    }

    public async Task<LoginResult> RegisterClientAsync(RegisterClientPortalUserCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = IdentityQueries.NormalizeEmail(command.Email);
        var exists = await dbContext.Set<IdentityUser>().AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"User with email '{command.Email}' already exists.");
        }

        var onboarding = await clientOnboardingService.CreateClientPortalProfileAsync(
            new CreateClientPortalProfileCommand(
                command.DisplayName,
                command.FirstName,
                command.LastName,
                command.Email,
                command.Phone,
                command.Instagram),
            cancellationToken);

        var utcNow = DateTime.UtcNow;
        var user = new IdentityUser
        {
            Id = Guid.NewGuid(),
            SubjectId = $"usr_{Guid.NewGuid():N}",
            Email = command.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            DisplayName = command.DisplayName.Trim(),
            PasswordHash = passwordHasher.Hash(command.Password),
            Status = UserStatusCodes.Active,
            ClientId = onboarding.ClientId,
            ContactPersonId = onboarding.ContactPersonId,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        dbContext.Set<IdentityUser>().Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var clientRole = await dbContext.Set<IdentityRole>()
            .SingleAsync(x => x.Code == RoleCodes.Client, cancellationToken);

        dbContext.Set<UserRoleAssignment>().Add(new UserRoleAssignment
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RoleId = clientRole.Id,
            ScopeType = "Global",
            ScopeId = null,
            AssignedAtUtc = utcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var token = await AuthenticateClientAsync(command.Email, command.Password, cancellationToken);
        return token ?? throw new InvalidOperationException("Client portal registration finished without an active login session.");
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

public sealed record RegisterClientPortalUserCommand(string DisplayName, string FirstName, string? LastName, string Email, string Password, string? Phone, string? Instagram);
