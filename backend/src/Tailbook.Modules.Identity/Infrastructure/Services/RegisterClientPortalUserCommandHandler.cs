using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Contracts;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public class RegisterClientPortalUserCommandHandler(
    AppDbContext dbContext,
    PasswordHasher passwordHasher,
    IClientOnboardingService clientOnboardingService
) : IRegisterClientPortalUserHandler
{
    public async Task<ErrorOr<bool>> ExecuteResultAsync(RegisterClientPortalUserCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = IdentityQueries.NormalizeEmail(command.Email);
        var exists = await dbContext.Set<IdentityUser>()
            .AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (exists)
        {
            return Error.Conflict("Identity.UserEmailExists", $"User with email '{command.Email}' already exists.");
        }

        var onboarding = await clientOnboardingService.CreateClientPortalProfileAsync(
            new CreateClientPortalProfileCommand(
                command.DisplayName,
                command.FirstName,
                command.LastName,
                command.Email,
                command.Phone,
                command.Instagram
            ),
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
        return true;
    }
}
