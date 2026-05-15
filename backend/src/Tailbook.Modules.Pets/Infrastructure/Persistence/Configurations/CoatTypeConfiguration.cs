using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Pets.Infrastructure.Persistence.Configurations;

public sealed class CoatTypeConfiguration : IEntityTypeConfiguration<CoatType>
{
    public void Configure(EntityTypeBuilder<CoatType> builder)
    {
        builder.ToTable("coat_types", "pets");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasOne<AnimalType>().WithMany().HasForeignKey(x => x.AnimalTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
