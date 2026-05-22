using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Persistence.Configurations;

public sealed class VisitExecutionItemConfiguration : IEntityTypeConfiguration<VisitExecutionItem>
{
    public void Configure(EntityTypeBuilder<VisitExecutionItem> builder)
    {
        builder.ToTable("visit_execution_items", "visitops");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ItemType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.OfferCodeSnapshot).HasMaxLength(64).IsRequired();
            builder.Property(x => x.OfferDisplayNameSnapshot).HasMaxLength(200).IsRequired();
            builder.Property(x => x.PriceAmountSnapshot).HasPrecision(18, 2).IsRequired();
            builder.HasIndex(x => x.VisitId);
            builder.HasIndex(x => x.AppointmentItemId).IsUnique();
            builder.HasMany(x => x.PerformedProcedures).WithOne().HasForeignKey(x => x.VisitExecutionItemId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.PerformedProcedures).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasMany(x => x.SkippedComponents).WithOne().HasForeignKey(x => x.VisitExecutionItemId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.SkippedComponents).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
