using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class IdentityDeviceTrustConfiguration : IEntityTypeConfiguration<IdentityDeviceTrust>
{
    public void Configure(EntityTypeBuilder<IdentityDeviceTrust> builder)
    {
        builder.ToTable("iam_device_trusts", "iam");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DeviceTokenHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Surface).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(128);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.Surface });
        builder.HasIndex(x => x.DeviceTokenHash).IsUnique();
    }
}
