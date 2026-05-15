using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class IdentityPermissionConfiguration : IEntityTypeConfiguration<IdentityPermission>
{
    public void Configure(EntityTypeBuilder<IdentityPermission> builder)
    {
        builder.ToTable("iam_permissions", "iam");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).HasMaxLength(128).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();

            builder.HasIndex(x => x.Code).IsUnique();
    }
}
