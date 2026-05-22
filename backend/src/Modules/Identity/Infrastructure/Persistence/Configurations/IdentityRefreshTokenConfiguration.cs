using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class IdentityRefreshTokenConfiguration : IEntityTypeConfiguration<IdentityRefreshToken>
{
    public void Configure(EntityTypeBuilder<IdentityRefreshToken> builder)
    {
        builder.ToTable("iam_refresh_tokens", "iam");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ExpiresAt).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.RevokedAt);
            builder.Property(x => x.ReplacedByTokenId);

            builder.HasIndex(x => x.TokenHash).IsUnique();
            builder.HasIndex(x => x.UserId);
    }
}
