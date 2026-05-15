using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class CommercialOfferConfiguration : IEntityTypeConfiguration<CommercialOffer>
{
    public void Configure(EntityTypeBuilder<CommercialOffer> builder)
    {
        builder.ToTable("catalog_offers", "catalog");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.Property(x => x.OfferType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => new { x.OfferType, x.DisplayName });
            builder.HasMany(x => x.Versions).WithOne().HasForeignKey(x => x.OfferId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
