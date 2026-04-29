using Microsoft.EntityFrameworkCore;
using Tailbook.Modules.Notifications.Domain;

namespace Tailbook.Modules.Notifications.Infrastructure;

public static class NotificationsModelConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationTemplate>(builder =>
        {
            builder.ToTable("notification_templates", "notifications");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Channel).HasMaxLength(32).IsRequired();
            builder.Property(x => x.SubjectTemplate).HasMaxLength(256).IsRequired();
            builder.Property(x => x.BodyTemplate).HasColumnType("text").IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<NotificationJob>(builder =>
        {
            builder.ToTable("notification_jobs", "notifications");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SourceEventType).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Channel).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Recipient).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Subject).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Body).HasColumnType("text").IsRequired();
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.Property(x => x.LastErrorMessage).HasMaxLength(1024);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.SourceEventMessageId);
        });

        modelBuilder.Entity<NotificationDeliveryAttempt>(builder =>
        {
            builder.ToTable("notification_delivery_attempts", "notifications");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.Property(x => x.ErrorMessage).HasMaxLength(1024);
            builder.HasIndex(x => new { x.NotificationJobId, x.AttemptNo }).IsUnique();
        });
    }
}
