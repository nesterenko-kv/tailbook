using Microsoft.EntityFrameworkCore;
using Tailbook.Modules.Staff.Domain;

namespace Tailbook.Modules.Staff.Infrastructure;

public static class StaffModelConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Groomer>(builder =>
        {
            builder.ToTable("staff_groomers", "staff");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Active).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasIndex(x => x.UserId).IsUnique();
            builder.HasIndex(x => new { x.Active, x.DisplayName });
        });

        modelBuilder.Entity<GroomerCapability>(builder =>
        {
            builder.ToTable("staff_groomer_capabilities", "staff");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CapabilityMode).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasIndex(x => x.GroomerId);
            builder.HasIndex(x => new { x.GroomerId, x.OfferId, x.CapabilityMode });
            builder.HasOne<Groomer>().WithMany().HasForeignKey(x => x.GroomerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkingSchedule>(builder =>
        {
            builder.ToTable("staff_working_schedules", "staff");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Weekday).IsRequired();
            builder.Property(x => x.StartLocalTime).IsRequired();
            builder.Property(x => x.EndLocalTime).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasIndex(x => new { x.GroomerId, x.Weekday }).IsUnique();
            builder.HasOne<Groomer>().WithMany().HasForeignKey(x => x.GroomerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TimeBlock>(builder =>
        {
            builder.ToTable("staff_time_blocks", "staff");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ReasonCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.StartAtUtc).IsRequired();
            builder.Property(x => x.EndAtUtc).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasIndex(x => new { x.GroomerId, x.StartAtUtc, x.EndAtUtc });
            builder.HasOne<Groomer>().WithMany().HasForeignKey(x => x.GroomerId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
