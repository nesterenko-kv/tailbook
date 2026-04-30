using Microsoft.EntityFrameworkCore;

namespace Tailbook.Modules.Audit.Infrastructure.Persistence.Configurations;

public static class AuditModelConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessAuditEntry>(builder =>
        {
            builder.ToTable("access_audit_entries", "audit");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ResourceType).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ResourceId).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ActionCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.HappenedAtUtc).IsRequired();

            builder.HasIndex(x => x.HappenedAtUtc);
            builder.HasIndex(x => new { x.ResourceType, x.ResourceId, x.HappenedAtUtc });
        });

        modelBuilder.Entity<AuditEntry>(builder =>
        {
            builder.ToTable("audit_entries", "audit");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ModuleCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
            builder.Property(x => x.EntityId).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ActionCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.HappenedAtUtc).IsRequired();
            builder.Property(x => x.BeforeJson).HasColumnType("jsonb");
            builder.Property(x => x.AfterJson).HasColumnType("jsonb");

            builder.HasIndex(x => x.ActorUserId);
            builder.HasIndex(x => new { x.ModuleCode, x.EntityType, x.EntityId, x.HappenedAtUtc });
        });
    }
}
