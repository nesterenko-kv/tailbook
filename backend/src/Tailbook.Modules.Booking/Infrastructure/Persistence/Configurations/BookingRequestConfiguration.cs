using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Booking.Infrastructure.Persistence.Configurations;

public sealed class BookingRequestConfiguration : IEntityTypeConfiguration<BookingRequest>
{
    public void Configure(EntityTypeBuilder<BookingRequest> builder)
    {
        builder.ToTable("booking_requests", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Channel).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.Property(x => x.SelectionMode).HasMaxLength(32);
            builder.Property(x => x.GuestIntakeJson).HasColumnType("jsonb");
            builder.Property(x => x.PreferredTimeJson).HasColumnType("jsonb");
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.VersionNo).IsRequired().IsConcurrencyToken();
            builder.HasIndex(x => x.PetId);
            builder.HasIndex(x => x.ClientId);
            builder.HasIndex(x => x.PreferredGroomerId);
            builder.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
