using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Persistence.Configurations;

public sealed class VisitPriceAdjustmentConfiguration : IEntityTypeConfiguration<VisitPriceAdjustment>
{
    public void Configure(EntityTypeBuilder<VisitPriceAdjustment> builder)
    {
        builder.ToTable("visit_price_adjustments", "visitops");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.TargetItemId);
            builder.Property(x => x.ReasonCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(1000);
            builder.HasIndex(x => x.VisitId);
            builder.HasIndex(x => x.CreatedAt);
    }
}
