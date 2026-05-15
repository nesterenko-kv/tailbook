using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Booking.Infrastructure.Persistence.Configurations;

public sealed class DurationSnapshotLineConfiguration : IEntityTypeConfiguration<DurationSnapshotLine>
{
    public void Configure(EntityTypeBuilder<DurationSnapshotLine> builder)
    {
        builder.ToTable("duration_snapshot_lines", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.LineType).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Label).HasMaxLength(256).IsRequired();
            builder.HasIndex(x => new { x.DurationSnapshotId, x.SequenceNo }).IsUnique();
            builder.HasOne<DurationSnapshot>().WithMany().HasForeignKey(x => x.DurationSnapshotId).OnDelete(DeleteBehavior.Cascade);
    }
}
