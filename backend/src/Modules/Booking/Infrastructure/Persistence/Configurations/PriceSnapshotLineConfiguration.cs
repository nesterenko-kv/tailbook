using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Booking.Infrastructure.Persistence.Configurations;

public sealed class PriceSnapshotLineConfiguration : IEntityTypeConfiguration<PriceSnapshotLine>
{
    public void Configure(EntityTypeBuilder<PriceSnapshotLine> builder)
    {
        builder.ToTable("price_snapshot_lines", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.LineType).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Label).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            builder.HasIndex(x => new { x.PriceSnapshotId, x.SequenceNo }).IsUnique();
            builder.HasOne<PriceSnapshot>().WithMany().HasForeignKey(x => x.PriceSnapshotId).OnDelete(DeleteBehavior.Cascade);
    }
}
