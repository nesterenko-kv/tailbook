using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages", "integration");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MessageId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ConsumerName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(512).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired().HasDefaultValue("Received");
        builder.Property(x => x.ReceivedAt).IsRequired();
        builder.Property(x => x.ProcessedAt);
        builder.Property(x => x.RetryCount).HasDefaultValue(0);
        builder.Property(x => x.LastError).HasMaxLength(2048);
        builder.Property(x => x.NextRetryAt);
        builder.Property(x => x.IsPoisoned).HasDefaultValue(false);
        builder.Property(x => x.PoisonedAt);

        builder.HasIndex(x => new { x.MessageId, x.ConsumerName }).IsUnique();
        builder.HasIndex(x => new { x.Status, x.NextRetryAt });
        builder.HasIndex(x => new { x.ConsumerName, x.Status, x.ReceivedAt });
    }
}
