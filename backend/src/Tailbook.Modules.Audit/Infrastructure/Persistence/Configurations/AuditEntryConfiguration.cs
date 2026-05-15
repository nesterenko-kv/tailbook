using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Audit.Infrastructure.Persistence.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries", "audit");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ModuleCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
            builder.Property(x => x.EntityId).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ActionCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.HappenedAt).IsRequired();
            builder.Property(x => x.BeforeJson).HasColumnType("jsonb");
            builder.Property(x => x.AfterJson).HasColumnType("jsonb");

            builder.HasIndex(x => x.ActorUserId);
            builder.HasIndex(x => new { x.ModuleCode, x.EntityType, x.EntityId, x.HappenedAt });
    }
}
