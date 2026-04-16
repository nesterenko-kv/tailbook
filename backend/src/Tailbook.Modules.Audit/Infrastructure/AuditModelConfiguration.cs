using Microsoft.EntityFrameworkCore;
using Tailbook.Modules.Audit.Domain;

namespace Tailbook.Modules.Audit.Infrastructure;

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
    }
}
