using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Persistence.Configurations;

public sealed class VisitSkippedComponentConfiguration : IEntityTypeConfiguration<VisitSkippedComponent>
{
    public void Configure(EntityTypeBuilder<VisitSkippedComponent> builder)
    {
        builder.ToTable("visit_skipped_components", "visitops");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ProcedureCodeSnapshot).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ProcedureNameSnapshot).HasMaxLength(200).IsRequired();
            builder.Property(x => x.OmissionReasonCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(1000);
            builder.HasIndex(x => x.VisitExecutionItemId);
            builder.HasIndex(x => new { x.VisitExecutionItemId, x.OfferVersionComponentId }).IsUnique();
    }
}
