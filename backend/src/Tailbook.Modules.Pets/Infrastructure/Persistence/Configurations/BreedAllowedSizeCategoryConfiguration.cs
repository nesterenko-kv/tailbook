using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Pets.Infrastructure.Persistence.Configurations;

public sealed class BreedAllowedSizeCategoryConfiguration : IEntityTypeConfiguration<BreedAllowedSizeCategory>
{
    public void Configure(EntityTypeBuilder<BreedAllowedSizeCategory> builder)
    {
        builder.ToTable("breed_allowed_size_categories", "pets");
            builder.HasKey(x => new { x.BreedId, x.SizeCategoryId });
            builder.HasIndex(x => x.SizeCategoryId);
            builder.HasOne<Breed>().WithMany().HasForeignKey(x => x.BreedId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<SizeCategory>().WithMany().HasForeignKey(x => x.SizeCategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
