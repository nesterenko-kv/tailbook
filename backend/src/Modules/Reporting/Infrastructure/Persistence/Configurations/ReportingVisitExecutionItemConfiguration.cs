using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Reporting.Infrastructure.Persistence.Configurations;

public sealed class ReportingVisitExecutionItemConfiguration : IEntityTypeConfiguration<ReportingVisitExecutionItem>
{
    public void Configure(EntityTypeBuilder<ReportingVisitExecutionItem> builder)
    {
        builder.ToView("visit_execution_items", "visitops");
            builder.HasKey(x => x.Id);
    }
}
