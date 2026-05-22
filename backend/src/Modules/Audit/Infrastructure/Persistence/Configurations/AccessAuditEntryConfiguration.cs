using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Audit.Infrastructure.Persistence.Configurations;

public sealed class AccessAuditEntryConfiguration : IEntityTypeConfiguration<AccessAuditEntry>
{
    public void Configure(EntityTypeBuilder<AccessAuditEntry> builder)
    {
        builder.ToTable("access_audit_entries", "audit");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ResourceType).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ResourceId).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ActionCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.HappenedAt).IsRequired();

            builder.HasIndex(x => x.HappenedAt);
            builder.HasIndex(x => new { x.ResourceType, x.ResourceId, x.HappenedAt });
    }
}
