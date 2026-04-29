using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Notifications.Domain;

namespace Tailbook.Modules.Notifications.Application;

public sealed class NotificationTemplateSeeder : IDataSeeder
{
    public int Order => 50;

    public async Task SeedAsync(AppDbContext dbContext, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var existingCodes = await dbContext.Set<NotificationTemplate>().Select(x => x.Code).ToListAsync(cancellationToken);
        var utcNow = DateTime.UtcNow;
        var seed = new[]
        {
            new NotificationTemplate { Id = Guid.NewGuid(), Code = "APPOINTMENT_CREATED", DisplayName = "Appointment created", Channel = "LocalFile", SubjectTemplate = "Appointment created", BodyTemplate = "Appointment {{appointmentId}} was created for groomer {{groomerId}}.", IsActive = true, CreatedAtUtc = utcNow },
            new NotificationTemplate { Id = Guid.NewGuid(), Code = "VISIT_CLOSED", DisplayName = "Visit closed", Channel = "LocalFile", SubjectTemplate = "Visit closed", BodyTemplate = "Visit {{visitId}} was closed with final total {{finalTotalAmount}}.", IsActive = true, CreatedAtUtc = utcNow },
            new NotificationTemplate { Id = Guid.NewGuid(), Code = "PASSWORD_RESET_REQUESTED", DisplayName = "Password reset requested", Channel = "LocalFile", SubjectTemplate = "Tailbook password reset", BodyTemplate = "Password reset token for {{email}}: {{resetToken}}. Expires at {{expiresAtUtc}}.", IsActive = true, CreatedAtUtc = utcNow }
        };
        foreach (var item in seed.Where(x => !existingCodes.Contains(x.Code, StringComparer.OrdinalIgnoreCase)))
        {
            dbContext.Set<NotificationTemplate>().Add(item);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
