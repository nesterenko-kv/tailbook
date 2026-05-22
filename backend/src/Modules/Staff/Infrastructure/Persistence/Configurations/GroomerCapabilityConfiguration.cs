using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Staff.Infrastructure.Persistence.Configurations;

public sealed class GroomerCapabilityConfiguration : IEntityTypeConfiguration<GroomerCapability>
{
    public void Configure(EntityTypeBuilder<GroomerCapability> builder)
    {
        builder.ToTable("staff_groomer_capabilities", "staff");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CapabilityMode).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => x.GroomerId);
            builder.HasIndex(x => new { x.GroomerId, x.OfferId, x.CapabilityMode });
            builder.HasOne<Groomer>().WithMany().HasForeignKey(x => x.GroomerId).OnDelete(DeleteBehavior.Cascade);
    }
}
