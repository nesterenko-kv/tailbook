using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Pets.Infrastructure.Persistence.Configurations;

public sealed class SizeCategoryConfiguration : IEntityTypeConfiguration<SizeCategory>
{
    public void Configure(EntityTypeBuilder<SizeCategory> builder)
    {
        builder.ToTable("size_categories", "pets");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
            builder.Property(x => x.MinWeightKg).HasPrecision(10, 2);
            builder.Property(x => x.MaxWeightKg).HasPrecision(10, 2);
            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasOne<AnimalType>().WithMany().HasForeignKey(x => x.AnimalTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
