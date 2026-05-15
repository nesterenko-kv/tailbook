using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class PriceRuleConditionConfiguration : IEntityTypeConfiguration<PriceRuleCondition>
{
    public void Configure(EntityTypeBuilder<PriceRuleCondition> builder)
    {
            builder.ToTable("pricing_rule_conditions", "catalog");
            builder.HasKey(x => x.Id);
            builder.Ignore(x => x.SpecificityScore);
            builder.HasIndex(x => x.PriceRuleId).IsUnique();
            builder.HasIndex(x => new { x.AnimalTypeId, x.BreedId, x.BreedGroupId, x.CoatTypeId, x.SizeCategoryId });
            builder.HasOne<PriceRule>().WithOne(x => x.Condition).HasForeignKey<PriceRuleCondition>(x => x.PriceRuleId).OnDelete(DeleteBehavior.Cascade);
    }
}
