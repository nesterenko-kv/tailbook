using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Notifications.Infrastructure.Persistence.Configurations;

public sealed class NotificationJobConfiguration : IEntityTypeConfiguration<NotificationJob>
{
    public void Configure(EntityTypeBuilder<NotificationJob> builder)
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
            builder.HasIndex(x => x.NextAttemptAt);
            builder.HasIndex(x => x.SourceEventMessageId);
    }
}
