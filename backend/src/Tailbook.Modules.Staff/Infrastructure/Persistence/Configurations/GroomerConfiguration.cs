using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Staff.Infrastructure.Persistence.Configurations;

public sealed class GroomerConfiguration : IEntityTypeConfiguration<Groomer>
{
    public void Configure(EntityTypeBuilder<Groomer> builder)
    {
        builder.ToTable("staff_groomers", "staff");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Active).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.HasIndex(x => x.UserId).IsUnique();
            builder.HasIndex(x => new { x.Active, x.DisplayName });
    }
}
