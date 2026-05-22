using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class IdentityMfaFactorConfiguration : IEntityTypeConfiguration<IdentityMfaFactor>
{
    public void Configure(EntityTypeBuilder<IdentityMfaFactor> builder)
    {
        builder.ToTable("iam_mfa_factors", "iam");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FactorType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.Property(x => x.TargetEmail).HasMaxLength(256).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.EnabledAt);
            builder.Property(x => x.DisabledAt);

            builder.HasIndex(x => new { x.UserId, x.FactorType });
            builder.HasIndex(x => new { x.Status, x.FactorType });
    }
}
