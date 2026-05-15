using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Staff.Infrastructure.Persistence.Configurations;

public sealed class TimeBlockConfiguration : IEntityTypeConfiguration<TimeBlock>
{
    public void Configure(EntityTypeBuilder<TimeBlock> builder)
    {
        builder.ToTable("staff_time_blocks", "staff");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ReasonCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.StartAt).IsRequired();
            builder.Property(x => x.EndAt).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => new { x.GroomerId, x.StartAt, x.EndAt });
            builder.HasOne<Groomer>().WithMany().HasForeignKey(x => x.GroomerId).OnDelete(DeleteBehavior.Cascade);
    }
}
