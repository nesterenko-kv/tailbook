using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Customer.Infrastructure.Persistence.Configurations;

public sealed class PetContactLinkConfiguration : IEntityTypeConfiguration<PetContactLink>
{
    public void Configure(EntityTypeBuilder<PetContactLink> builder)
    {
        builder.ToTable("crm_pet_contact_links", "crm");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.RoleCodes).HasMaxLength(512).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.HasIndex(x => x.PetId);
            builder.HasIndex(x => x.ContactPersonId);
            builder.HasIndex(x => new { x.PetId, x.ContactPersonId }).IsUnique();
            builder.HasOne<ContactPerson>().WithMany().HasForeignKey(x => x.ContactPersonId).OnDelete(DeleteBehavior.Cascade);
    }
}
