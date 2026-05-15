using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Reporting.Infrastructure.Persistence.Configurations;

public sealed class ReportingVisitSkippedComponentConfiguration : IEntityTypeConfiguration<ReportingVisitSkippedComponent>
{
    public void Configure(EntityTypeBuilder<ReportingVisitSkippedComponent> builder)
    {
        builder.ToView("visit_skipped_components", "visitops");
            builder.HasKey(x => x.Id);
    }
}
