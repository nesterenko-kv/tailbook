using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Reporting.Infrastructure.Persistence.Configurations;

public sealed class ReportingDurationSnapshotConfiguration : IEntityTypeConfiguration<ReportingDurationSnapshot>
{
    public void Configure(EntityTypeBuilder<ReportingDurationSnapshot> builder)
    {
        builder.ToView("duration_snapshots", "booking");
            builder.HasKey(x => x.Id);
    }
}
