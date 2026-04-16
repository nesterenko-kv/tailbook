using Microsoft.EntityFrameworkCore;
using Tailbook.Modules.Customer.Domain;

namespace Tailbook.Modules.Customer.Infrastructure;

public static class CustomerModelConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(builder =>
        {
            builder.ToTable("crm_clients", "crm");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasIndex(x => x.DisplayName);
            builder.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<ContactPerson>(builder =>
        {
            builder.ToTable("crm_contact_persons", "crm");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.LastName).HasMaxLength(100);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.TrustLevel).HasMaxLength(32).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasIndex(x => x.ClientId);
            builder.HasOne<Client>().WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContactMethod>(builder =>
        {
            builder.ToTable("crm_contact_methods", "crm");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.MethodType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.NormalizedValue).HasMaxLength(256).IsRequired();
            builder.Property(x => x.DisplayValue).HasMaxLength(256).IsRequired();
            builder.Property(x => x.VerificationStatus).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasIndex(x => x.ContactPersonId);
            builder.HasIndex(x => new { x.ContactPersonId, x.MethodType, x.NormalizedValue }).IsUnique();
            builder.HasOne<ContactPerson>().WithMany().HasForeignKey(x => x.ContactPersonId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PetContactLink>(builder =>
        {
            builder.ToTable("crm_pet_contact_links", "crm");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.RoleCodes).HasMaxLength(512).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasIndex(x => x.PetId);
            builder.HasIndex(x => x.ContactPersonId);
            builder.HasIndex(x => new { x.PetId, x.ContactPersonId }).IsUnique();
            builder.HasOne<ContactPerson>().WithMany().HasForeignKey(x => x.ContactPersonId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
