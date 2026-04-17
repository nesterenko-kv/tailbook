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

        modelBuilder.Entity<BookingRequest>(builder =>
        {
            builder.ToTable("booking_requests", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Channel).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.Property(x => x.SelectionMode).HasMaxLength(32);
            builder.Property(x => x.GuestIntakeJson).HasColumnType("jsonb");
            builder.Property(x => x.PreferredTimeJson).HasColumnType("jsonb");
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.HasIndex(x => x.PetId);
            builder.HasIndex(x => x.ClientId);
            builder.HasIndex(x => x.PreferredGroomerId);
            builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        });

        modelBuilder.Entity<BookingRequestItem>(builder =>
        {
            builder.ToTable("booking_request_items", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ItemType).HasMaxLength(32);
            builder.Property(x => x.RequestedNotes).HasMaxLength(1000);
            builder.HasOne<BookingRequest>().WithMany().HasForeignKey(x => x.BookingRequestId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.BookingRequestId);
        });

        modelBuilder.Entity<Appointment>(builder =>
        {
            builder.ToTable("appointments", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.Property(x => x.CancellationReasonCode).HasMaxLength(64);
            builder.Property(x => x.CancellationNotes).HasMaxLength(1000);
            builder.Property(x => x.VersionNo).IsRequired();
            builder.HasIndex(x => new { x.GroomerId, x.StartAtUtc, x.EndAtUtc });
            builder.HasIndex(x => new { x.Status, x.StartAtUtc });
            builder.HasIndex(x => x.BookingRequestId).IsUnique(false);
        });

        modelBuilder.Entity<AppointmentItem>(builder =>
        {
            builder.ToTable("appointment_items", "booking");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ItemType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.OfferCodeSnapshot).HasMaxLength(64).IsRequired();
            builder.Property(x => x.OfferDisplayNameSnapshot).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Quantity).IsRequired();
            builder.HasOne<Appointment>().WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.AppointmentId);
            builder.HasIndex(x => x.PriceSnapshotId);
            builder.HasIndex(x => x.DurationSnapshotId);
        });
    }
}
