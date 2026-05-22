using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Customer.Infrastructure.Persistence.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("crm_clients", "crm");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.HasIndex(x => x.DisplayName);
            builder.HasIndex(x => x.Status);
            builder.HasMany(x => x.Contacts).WithOne().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.Contacts).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
