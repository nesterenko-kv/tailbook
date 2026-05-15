using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class IdentityPasswordResetTokenConfiguration : IEntityTypeConfiguration<IdentityPasswordResetToken>
{
    public void Configure(EntityTypeBuilder<IdentityPasswordResetToken> builder)
    {
        builder.ToTable("iam_password_reset_tokens", "iam");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ExpiresAt).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UsedAt);

            builder.HasIndex(x => x.TokenHash).IsUnique();
            builder.HasIndex(x => new { x.UserId, x.ExpiresAt });
    }
}
