using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Jobs;

public sealed class JobRecordConfiguration : IEntityTypeConfiguration<JobRecord>
{
    public void Configure(EntityTypeBuilder<JobRecord> builder)
    {
        builder.ToTable("Jobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QueueID).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CommandJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.ResultJson).HasColumnType("text");
        builder.Property(x => x.DequeueAfter).HasDefaultValue(DateTime.UnixEpoch);
        builder.HasIndex(x => x.TrackingID).IsUnique();
        builder.HasIndex(x => new { x.QueueID, x.IsComplete, x.ExecuteAfter, x.ExpireOn, x.DequeueAfter });
    }
}
