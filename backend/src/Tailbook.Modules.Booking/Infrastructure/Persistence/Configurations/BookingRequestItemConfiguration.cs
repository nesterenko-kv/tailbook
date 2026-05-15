using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Booking.Infrastructure.Persistence.Configurations;

public sealed class BookingRequestItemConfiguration : IEntityTypeConfiguration<BookingRequestItem>
{
    public void Configure(EntityTypeBuilder<BookingRequestItem> builder)
    {
        builder.ToTable("booking_request_items", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ItemType).HasMaxLength(32);
            builder.Property(x => x.RequestedNotes).HasMaxLength(1000);
            builder.HasOne<BookingRequest>().WithMany().HasForeignKey(x => x.BookingRequestId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.BookingRequestId);
    }
}
