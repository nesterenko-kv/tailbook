using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class DurationRuleConditionConfiguration : IEntityTypeConfiguration<DurationRuleCondition>
{
    public void Configure(EntityTypeBuilder<DurationRuleCondition> builder)
    {
            builder.ToTable("duration_rule_conditions", "catalog");
            builder.HasKey(x => x.Id);
            builder.Ignore(x => x.SpecificityScore);
            builder.HasIndex(x => x.DurationRuleId).IsUnique();
            builder.HasIndex(x => new { x.AnimalTypeId, x.BreedId, x.BreedGroupId, x.CoatTypeId, x.SizeCategoryId });
            builder.HasOne<DurationRule>().WithOne(x => x.Condition).HasForeignKey<DurationRuleCondition>(x => x.DurationRuleId).OnDelete(DeleteBehavior.Cascade);
    }
}
