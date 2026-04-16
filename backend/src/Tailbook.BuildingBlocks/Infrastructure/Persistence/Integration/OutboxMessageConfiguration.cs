using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", "integration");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ModuleCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(512).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.ProcessedAtUtc);

        builder.HasIndex(x => x.ProcessedAtUtc);
        builder.HasIndex(x => new { x.ModuleCode, x.OccurredAtUtc });
    }
}
