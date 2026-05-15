using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Pets.Infrastructure.Persistence.Configurations;

public sealed class PetPhotoConfiguration : IEntityTypeConfiguration<PetPhoto>
{
    public void Configure(EntityTypeBuilder<PetPhoto> builder)
    {
        builder.ToTable("pet_photos", "pets");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            builder.Property(x => x.FileName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => x.PetId);
            builder.HasOne<Pet>().WithMany().HasForeignKey(x => x.PetId).OnDelete(DeleteBehavior.Cascade);
    }
}
