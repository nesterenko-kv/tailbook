using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Persistence.Configurations;

public sealed class VisitPerformedProcedureConfiguration : IEntityTypeConfiguration<VisitPerformedProcedure>
{
    public void Configure(EntityTypeBuilder<VisitPerformedProcedure> builder)
    {
        builder.ToTable("visit_performed_procedures", "visitops");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ProcedureCodeSnapshot).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ProcedureNameSnapshot).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(1000);
            builder.HasIndex(x => x.VisitExecutionItemId);
            builder.HasIndex(x => new { x.VisitExecutionItemId, x.ProcedureId }).IsUnique();
    }
}
