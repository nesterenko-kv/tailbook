using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class DurationRuleConfiguration : IEntityTypeConfiguration<DurationRule>
{
    public void Configure(EntityTypeBuilder<DurationRule> builder)
    {
        builder.ToTable("duration_rules", "catalog");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => new { x.RuleSetId, x.OfferId, x.Priority });
            builder.HasOne<DurationRuleSet>().WithMany(x => x.Rules).HasForeignKey(x => x.RuleSetId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<CommercialOffer>().WithMany().HasForeignKey(x => x.OfferId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Condition).WithOne().HasForeignKey<DurationRuleCondition>(x => x.DurationRuleId).OnDelete(DeleteBehavior.Cascade);
    }
}
