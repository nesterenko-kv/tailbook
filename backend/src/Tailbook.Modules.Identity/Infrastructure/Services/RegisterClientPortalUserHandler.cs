using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public class RegisterClientPortalUserHandler(
    AppDbContext dbContext,
    PasswordHasher passwordHasher,
    IClientOnboardingService clientOnboardingService,
    TimeProvider timeProvider
) : IRegisterClientPortalUserHandler
{
    public async Task<ErrorOr<Created>> ExecuteResultAsync(RegisterClientPortalUserInput command,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = IdentityUseCases.NormalizeEmail(command.Email);
        var exists = await dbContext.Set<IdentityUser>()
            .AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (exists)
        {
            return IdentityErrors.UserEmailExists(command.Email);
        }

        var onboarding = await clientOnboardingService.CreateClientPortalProfileAsync(
            new CreateClientPortalProfileInput(
                command.DisplayName,
                command.FirstName,
                command.LastName,
                command.Email,
                command.Phone,
                command.Instagram
            ),
            cancellationToken);

        var utcNow = timeProvider.GetUtcNow();
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
            CreatedAt = utcNow,
            UpdatedAt = utcNow
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
            AssignedAt = utcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Created;
    }
}
