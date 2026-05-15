using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class IdentityMfaRecoveryCodeConfiguration : IEntityTypeConfiguration<IdentityMfaRecoveryCode>
{
    public void Configure(EntityTypeBuilder<IdentityMfaRecoveryCode> builder)
    {
        builder.ToTable("iam_mfa_recovery_codes", "iam");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BatchId).IsRequired();
        builder.Property(x => x.CodeHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.CodeSuffix).HasMaxLength(8).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ConsumedAt);
        builder.Property(x => x.ConsumedChallengeId);
        builder.Property(x => x.InvalidatedAt);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.BatchId);
        builder.HasIndex(x => new { x.UserId, x.ConsumedAt, x.InvalidatedAt });
    }
}
