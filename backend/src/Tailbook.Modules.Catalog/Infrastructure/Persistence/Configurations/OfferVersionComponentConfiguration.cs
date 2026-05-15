using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class OfferVersionComponentConfiguration : IEntityTypeConfiguration<OfferVersionComponent>
{
    public void Configure(EntityTypeBuilder<OfferVersionComponent> builder)
    {
        builder.ToTable("catalog_offer_version_components", "catalog");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ComponentRole).HasMaxLength(32).IsRequired();
            builder.Property(x => x.SequenceNo).IsRequired();
            builder.Property(x => x.DefaultExpected).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => new { x.OfferVersionId, x.SequenceNo }).IsUnique();
            builder.HasIndex(x => new { x.OfferVersionId, x.ProcedureId }).IsUnique();
            builder.HasOne<OfferVersion>().WithMany(x => x.Components).HasForeignKey(x => x.OfferVersionId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<ProcedureCatalogItem>().WithMany().HasForeignKey(x => x.ProcedureId).OnDelete(DeleteBehavior.Restrict);
    }
}
