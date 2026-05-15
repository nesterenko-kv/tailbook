using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Persistence.Configurations;

public sealed class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.ToTable("visits", "visitops");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.HasIndex(x => x.AppointmentId).IsUnique();
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CheckedInAt);
            builder.HasMany(x => x.ExecutionItems).WithOne().HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.ExecutionItems).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasMany(x => x.PriceAdjustments).WithOne().HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.PriceAdjustments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
