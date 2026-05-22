using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.ToTable("iam_role_assignments", "iam");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ScopeType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.ScopeId).HasMaxLength(64);
            builder.Property(x => x.AssignedAt).IsRequired();

            builder.HasIndex(x => new { x.UserId, x.RoleId, x.ScopeType, x.ScopeId }).IsUnique();
    }
}
