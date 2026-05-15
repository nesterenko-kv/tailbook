using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Pets.Infrastructure.Persistence.Configurations;

public sealed class BreedConfiguration : IEntityTypeConfiguration<Breed>
{
    public void Configure(EntityTypeBuilder<Breed> builder)
    {
        builder.ToTable("breeds", "pets");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
            builder.HasIndex(x => new { x.AnimalTypeId, x.Code }).IsUnique();
            builder.HasOne<AnimalType>().WithMany().HasForeignKey(x => x.AnimalTypeId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<BreedGroup>().WithMany().HasForeignKey(x => x.BreedGroupId).OnDelete(DeleteBehavior.Restrict);
    }
}
