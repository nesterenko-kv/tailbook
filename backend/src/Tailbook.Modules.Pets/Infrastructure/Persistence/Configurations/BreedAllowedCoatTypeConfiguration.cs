using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Pets.Infrastructure.Persistence.Configurations;

public sealed class BreedAllowedCoatTypeConfiguration : IEntityTypeConfiguration<BreedAllowedCoatType>
{
    public void Configure(EntityTypeBuilder<BreedAllowedCoatType> builder)
    {
        builder.ToTable("breed_allowed_coat_types", "pets");
            builder.HasKey(x => new { x.BreedId, x.CoatTypeId });
            builder.HasIndex(x => x.CoatTypeId);
            builder.HasOne<Breed>().WithMany().HasForeignKey(x => x.BreedId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<CoatType>().WithMany().HasForeignKey(x => x.CoatTypeId).OnDelete(DeleteBehavior.Cascade);
    }
}
