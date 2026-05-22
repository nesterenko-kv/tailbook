using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class DurationRuleSetConfiguration : IEntityTypeConfiguration<DurationRuleSet>
{
    public void Configure(EntityTypeBuilder<DurationRuleSet> builder)
    {
        builder.ToTable("duration_rule_sets", "catalog");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.Property(x => x.ValidFrom).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => x.VersionNo).IsUnique();
            builder.HasIndex(x => new { x.Status, x.ValidFrom });
            builder.HasMany(x => x.Rules).WithOne().HasForeignKey(x => x.RuleSetId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.Rules).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
