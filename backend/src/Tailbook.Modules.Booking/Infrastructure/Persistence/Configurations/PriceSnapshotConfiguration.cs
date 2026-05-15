using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Booking.Infrastructure.Persistence.Configurations;

public sealed class PriceSnapshotConfiguration : IEntityTypeConfiguration<PriceSnapshot>
{
    public void Configure(EntityTypeBuilder<PriceSnapshot> builder)
    {
        builder.ToTable("price_snapshots", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SnapshotType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
            builder.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => new { x.SnapshotType, x.CreatedAt });
    }
}
