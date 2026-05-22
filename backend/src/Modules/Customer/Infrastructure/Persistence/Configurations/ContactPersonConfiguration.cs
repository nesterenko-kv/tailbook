using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Customer.Infrastructure.Persistence.Configurations;

public sealed class ContactPersonConfiguration : IEntityTypeConfiguration<ContactPerson>
{
    public void Configure(EntityTypeBuilder<ContactPerson> builder)
    {
        builder.ToTable("crm_contact_persons", "crm");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.LastName).HasMaxLength(100);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.TrustLevel).HasMaxLength(32).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.HasIndex(x => x.ClientId);
            builder.HasMany(x => x.Methods).WithOne().HasForeignKey(x => x.ContactPersonId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.Methods).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
