using Microsoft.EntityFrameworkCore;
using Tailbook.Modules.Booking.Domain;

namespace Tailbook.Modules.Booking.Infrastructure;

public static class BookingModelConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PriceSnapshot>(builder =>
        {
            builder.ToTable("price_snapshots", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SnapshotType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
            builder.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasIndex(x => x.CreatedAtUtc);
            builder.HasIndex(x => new { x.SnapshotType, x.CreatedAtUtc });
        });

        modelBuilder.Entity<PriceSnapshotLine>(builder =>
        {
            builder.ToTable("price_snapshot_lines", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.LineType).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Label).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            builder.HasIndex(x => new { x.PriceSnapshotId, x.SequenceNo }).IsUnique();
            builder.HasOne<PriceSnapshot>().WithMany().HasForeignKey(x => x.PriceSnapshotId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DurationSnapshot>(builder =>
        {
            builder.ToTable("duration_snapshots", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<DurationSnapshotLine>(builder =>
        {
            builder.ToTable("duration_snapshot_lines", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.LineType).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Label).HasMaxLength(256).IsRequired();
            builder.HasIndex(x => new { x.DurationSnapshotId, x.SequenceNo }).IsUnique();
            builder.HasOne<DurationSnapshot>().WithMany().HasForeignKey(x => x.DurationSnapshotId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
