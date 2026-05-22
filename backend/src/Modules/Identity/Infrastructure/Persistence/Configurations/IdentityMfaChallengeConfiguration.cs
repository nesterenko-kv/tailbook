using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class IdentityMfaChallengeConfiguration : IEntityTypeConfiguration<IdentityMfaChallenge>
{
    public void Configure(EntityTypeBuilder<IdentityMfaChallenge> builder)
    {
        builder.ToTable("iam_mfa_challenges", "iam");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FactorType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CodeHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ConsumedAt);
        builder.Property(x => x.InvalidatedAt);
        builder.Property(x => x.FailedAttemptCount).IsRequired();
        builder.Property(x => x.LastFailedAt);
        builder.Property(x => x.RequestIpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.FactorId);
        builder.HasIndex(x => new { x.UserId, x.FactorType, x.ExpiresAt });
    }
}
