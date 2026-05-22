using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class PriceRuleConfiguration : IEntityTypeConfiguration<PriceRule>
{
    public void Configure(EntityTypeBuilder<PriceRule> builder)
    {
        builder.ToTable("pricing_rules", "catalog");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ActionType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
            builder.Property(x => x.FixedAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => new { x.RuleSetId, x.OfferId, x.Priority });
            builder.HasOne<PriceRuleSet>().WithMany(x => x.Rules).HasForeignKey(x => x.RuleSetId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<CommercialOffer>().WithMany().HasForeignKey(x => x.OfferId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Condition).WithOne().HasForeignKey<PriceRuleCondition>(x => x.PriceRuleId).OnDelete(DeleteBehavior.Cascade);
    }
}
