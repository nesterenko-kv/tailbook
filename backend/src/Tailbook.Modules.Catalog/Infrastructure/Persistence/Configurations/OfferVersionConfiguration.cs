using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class OfferVersionConfiguration : IEntityTypeConfiguration<OfferVersion>
{
    public void Configure(EntityTypeBuilder<OfferVersion> builder)
    {
        builder.ToTable("catalog_offer_versions", "catalog");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.Property(x => x.PolicyText).HasMaxLength(4000);
            builder.Property(x => x.ChangeNote).HasMaxLength(1000);
            builder.Property(x => x.ValidFrom).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.PublishedAt);
            builder.Ignore(x => x.IsDraft);
            builder.HasIndex(x => new { x.OfferId, x.VersionNo }).IsUnique();
            builder.HasIndex(x => new { x.OfferId, x.Status });
            builder.HasOne<CommercialOffer>().WithMany(x => x.Versions).HasForeignKey(x => x.OfferId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.Components).WithOne().HasForeignKey(x => x.OfferVersionId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.Components).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
