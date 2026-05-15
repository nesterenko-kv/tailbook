using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Booking.Infrastructure.Persistence.Configurations;

public sealed class AppointmentItemConfiguration : IEntityTypeConfiguration<AppointmentItem>
{
    public void Configure(EntityTypeBuilder<AppointmentItem> builder)
    {
        builder.ToTable("appointment_items", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ItemType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.OfferCodeSnapshot).HasMaxLength(64).IsRequired();
            builder.Property(x => x.OfferDisplayNameSnapshot).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Quantity).IsRequired();
            builder.HasIndex(x => x.AppointmentId);
            builder.HasIndex(x => x.PriceSnapshotId);
            builder.HasIndex(x => x.DurationSnapshotId);
    }
}
