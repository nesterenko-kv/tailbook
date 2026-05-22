using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Notifications.Infrastructure.Seeding;

public sealed class NotificationTemplateSeeder(TimeProvider timeProvider) : IDataSeeder
{
    public int Order => 50;

    public async Task SeedAsync(AppDbContext dbContext, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var existingCodes = await dbContext.Set<NotificationTemplate>().Select(x => x.Code).ToListAsync(cancellationToken);
        var utcNow = timeProvider.GetUtcNow();
        var seed = new[]
        {
            new NotificationTemplate { Id = Guid.NewGuid(), Code = "APPOINTMENT_CREATED", DisplayName = "Appointment created", Channel = "LocalFile", SubjectTemplate = "Appointment created", BodyTemplate = "Appointment {{appointmentId}} was created for groomer {{groomerId}}.", IsActive = true, CreatedAt = utcNow },
            new NotificationTemplate { Id = Guid.NewGuid(), Code = "VISIT_CLOSED", DisplayName = "Visit closed", Channel = "LocalFile", SubjectTemplate = "Visit closed", BodyTemplate = "Visit {{visitId}} was closed with final total {{finalTotalAmount}}.", IsActive = true, CreatedAt = utcNow },
            new NotificationTemplate { Id = Guid.NewGuid(), Code = "PASSWORD_RESET_REQUESTED", DisplayName = "Password reset requested", Channel = "LocalFile", SubjectTemplate = "Tailbook password reset", BodyTemplate = "Use this password reset link for {{email}}: {{resetLink}}. Expires at {{expiresAt}}.", IsActive = true, CreatedAt = utcNow },
            new NotificationTemplate { Id = Guid.NewGuid(), Code = "MFA_EMAIL_OTP_CHALLENGE", DisplayName = "MFA email OTP challenge", Channel = "LocalFile", SubjectTemplate = "Tailbook sign-in code", BodyTemplate = "Use sign-in code {{code}} for {{email}}. Expires at {{expiresAt}}.", IsActive = true, CreatedAt = utcNow }
        };
        foreach (var item in seed.Where(x => !existingCodes.Contains(x.Code, StringComparer.OrdinalIgnoreCase)))
        {
            dbContext.Set<NotificationTemplate>().Add(item);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
