using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Booking.Infrastructure.Persistence.Configurations;

public sealed class DurationSnapshotConfiguration : IEntityTypeConfiguration<DurationSnapshot>
{
    public void Configure(EntityTypeBuilder<DurationSnapshot> builder)
    {
        builder.ToTable("duration_snapshots", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => x.CreatedAt);
    }
}
