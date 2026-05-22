using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Customer.Infrastructure.Persistence.Configurations;

public sealed class ContactMethodConfiguration : IEntityTypeConfiguration<ContactMethod>
{
    public void Configure(EntityTypeBuilder<ContactMethod> builder)
    {
        builder.ToTable("crm_contact_methods", "crm");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.MethodType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.NormalizedValue).HasMaxLength(256).IsRequired();
            builder.Property(x => x.DisplayValue).HasMaxLength(256).IsRequired();
            builder.Property(x => x.VerificationStatus).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.HasIndex(x => x.ContactPersonId);
            builder.HasIndex(x => new { x.ContactPersonId, x.MethodType, x.NormalizedValue }).IsUnique();
    }
}
