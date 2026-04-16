using Microsoft.EntityFrameworkCore;
using Tailbook.Modules.Pets.Domain;

namespace Tailbook.Modules.Pets.Infrastructure;

public static class PetsModelConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnimalType>(builder =>
        {
            builder.ToTable("animal_types", "pets");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<BreedGroup>(builder =>
        {
            builder.ToTable("breed_groups", "pets");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
            builder.HasIndex(x => new { x.AnimalTypeId, x.Code }).IsUnique();
            builder.HasOne<AnimalType>().WithMany().HasForeignKey(x => x.AnimalTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Breed>(builder =>
        {
            builder.ToTable("breeds", "pets");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
            builder.HasIndex(x => new { x.AnimalTypeId, x.Code }).IsUnique();
            builder.HasOne<AnimalType>().WithMany().HasForeignKey(x => x.AnimalTypeId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<BreedGroup>().WithMany().HasForeignKey(x => x.BreedGroupId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CoatType>(builder =>
        {
            builder.ToTable("coat_types", "pets");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasOne<AnimalType>().WithMany().HasForeignKey(x => x.AnimalTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SizeCategory>(builder =>
        {
            builder.ToTable("size_categories", "pets");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
            builder.Property(x => x.MinWeightKg).HasPrecision(10, 2);
            builder.Property(x => x.MaxWeightKg).HasPrecision(10, 2);
            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasOne<AnimalType>().WithMany().HasForeignKey(x => x.AnimalTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pet>(builder =>
        {
            builder.ToTable("pets", "pets");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
            builder.Property(x => x.WeightKg).HasPrecision(10, 2);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasIndex(x => x.ClientId);
            builder.HasIndex(x => x.Name);
            builder.HasOne<AnimalType>().WithMany().HasForeignKey(x => x.AnimalTypeId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Breed>().WithMany().HasForeignKey(x => x.BreedId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<CoatType>().WithMany().HasForeignKey(x => x.CoatTypeId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SizeCategory>().WithMany().HasForeignKey(x => x.SizeCategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PetPhoto>(builder =>
        {
            builder.ToTable("pet_photos", "pets");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            builder.Property(x => x.FileName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasIndex(x => x.PetId);
            builder.HasOne<Pet>().WithMany().HasForeignKey(x => x.PetId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
