using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Pets.Infrastructure.Persistence.Configurations;

public sealed class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("pets", "pets");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
            builder.Property(x => x.WeightKg).HasPrecision(10, 2);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.HasIndex(x => x.ClientId);
            builder.HasIndex(x => x.Name);
            builder.HasOne<AnimalType>().WithMany().HasForeignKey(x => x.AnimalTypeId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Breed>().WithMany().HasForeignKey(x => x.BreedId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<CoatType>().WithMany().HasForeignKey(x => x.CoatTypeId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SizeCategory>().WithMany().HasForeignKey(x => x.SizeCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
