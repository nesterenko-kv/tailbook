using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Booking.Infrastructure.Persistence.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.Property(x => x.CancellationReasonCode).HasMaxLength(64);
            builder.Property(x => x.CancellationNotes).HasMaxLength(1000);
            builder.Property(x => x.VersionNo).IsRequired().IsConcurrencyToken();
            builder.Ignore(x => x.Period);
            builder.HasIndex(x => new { x.PetId, x.StartAt });
            builder.HasIndex(x => new { x.GroomerId, x.StartAt, x.EndAt });
            builder.HasIndex(x => new { x.Status, x.StartAt });
            builder.HasIndex(x => x.BookingRequestId)
                .IsUnique()
                .HasFilter("\"BookingRequestId\" IS NOT NULL");
            builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
