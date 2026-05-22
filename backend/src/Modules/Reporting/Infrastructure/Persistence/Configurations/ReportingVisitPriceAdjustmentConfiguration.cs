using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Reporting.Infrastructure.Persistence.Configurations;

public sealed class ReportingVisitPriceAdjustmentConfiguration : IEntityTypeConfiguration<ReportingVisitPriceAdjustment>
{
    public void Configure(EntityTypeBuilder<ReportingVisitPriceAdjustment> builder)
    {
        builder.ToView("visit_price_adjustments", "visitops");
            builder.HasKey(x => x.Id);
    }
}
