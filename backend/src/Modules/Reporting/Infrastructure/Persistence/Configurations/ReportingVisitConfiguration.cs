using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Reporting.Infrastructure.Persistence.Configurations;

public sealed class ReportingVisitConfiguration : IEntityTypeConfiguration<ReportingVisit>
{
    public void Configure(EntityTypeBuilder<ReportingVisit> builder)
    {
        builder.ToView("visits", "visitops");
            builder.HasKey(x => x.Id);
    }
}
