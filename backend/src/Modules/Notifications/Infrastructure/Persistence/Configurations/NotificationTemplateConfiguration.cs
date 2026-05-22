using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Notifications.Infrastructure.Persistence.Configurations;

public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates", "notifications");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Channel).HasMaxLength(32).IsRequired();
            builder.Property(x => x.SubjectTemplate).HasMaxLength(256).IsRequired();
            builder.Property(x => x.BodyTemplate).HasColumnType("text").IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();
    }
}
