using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Reporting.Infrastructure.Persistence.Configurations;

public sealed class ReportingPriceSnapshotConfiguration : IEntityTypeConfiguration<ReportingPriceSnapshot>
{
    public void Configure(EntityTypeBuilder<ReportingPriceSnapshot> builder)
    {
        builder.ToView("price_snapshots", "booking");
            builder.HasKey(x => x.Id);
    }
}
