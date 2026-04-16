using Microsoft.EntityFrameworkCore;
using Tailbook.Modules.Catalog.Domain;

namespace Tailbook.Modules.Catalog.Infrastructure;

public static class CatalogModelConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommercialOffer>(builder =>
        {
            builder.ToTable("catalog_offers", "catalog");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.Property(x => x.OfferType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => new { x.OfferType, x.DisplayName });
        });

        modelBuilder.Entity<OfferVersion>(builder =>
        {
            builder.ToTable("catalog_offer_versions", "catalog");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.Property(x => x.PolicyText).HasMaxLength(4000);
            builder.Property(x => x.ChangeNote).HasMaxLength(1000);
            builder.Property(x => x.ValidFromUtc).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.PublishedAtUtc);
            builder.HasIndex(x => new { x.OfferId, x.VersionNo }).IsUnique();
            builder.HasIndex(x => new { x.OfferId, x.Status });
            builder.HasOne<CommercialOffer>().WithMany().HasForeignKey(x => x.OfferId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcedureCatalogItem>(builder =>
        {
            builder.ToTable("catalog_procedures", "catalog");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<OfferVersionComponent>(builder =>
        {
            builder.ToTable("catalog_offer_version_components", "catalog");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ComponentRole).HasMaxLength(32).IsRequired();
            builder.Property(x => x.SequenceNo).IsRequired();
            builder.Property(x => x.DefaultExpected).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasIndex(x => new { x.OfferVersionId, x.SequenceNo }).IsUnique();
            builder.HasIndex(x => new { x.OfferVersionId, x.ProcedureId }).IsUnique();
            builder.HasOne<OfferVersion>().WithMany().HasForeignKey(x => x.OfferVersionId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<ProcedureCatalogItem>().WithMany().HasForeignKey(x => x.ProcedureId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
