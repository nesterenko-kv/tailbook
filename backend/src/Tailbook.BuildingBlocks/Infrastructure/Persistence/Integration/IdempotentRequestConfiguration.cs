using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

public sealed class IdempotentRequestConfiguration : IEntityTypeConfiguration<IdempotentRequest>
{
    public void Configure(EntityTypeBuilder<IdempotentRequest> builder)
    {
        builder.ToTable("idempotent_requests", "integration");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ResponseBody).HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CompletedAt);
        builder.Property(x => x.ExpiresAt);

        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.HasIndex(x => x.ExpiresAt).HasFilter("\"ExpiresAt\" IS NOT NULL");
    }
}
