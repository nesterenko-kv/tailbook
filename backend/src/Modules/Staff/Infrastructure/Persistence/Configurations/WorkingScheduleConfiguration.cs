using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Staff.Infrastructure.Persistence.Configurations;

public sealed class WorkingScheduleConfiguration : IEntityTypeConfiguration<WorkingSchedule>
{
    public void Configure(EntityTypeBuilder<WorkingSchedule> builder)
    {
        builder.ToTable("staff_working_schedules", "staff");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Weekday).IsRequired();
            builder.Property(x => x.StartLocalTime).IsRequired();
            builder.Property(x => x.EndLocalTime).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.HasIndex(x => new { x.GroomerId, x.Weekday }).IsUnique();
            builder.HasOne<Groomer>().WithMany().HasForeignKey(x => x.GroomerId).OnDelete(DeleteBehavior.Cascade);
    }
}
