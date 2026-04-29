using Microsoft.EntityFrameworkCore;
using Tailbook.Modules.Identity.Domain;

namespace Tailbook.Modules.Identity.Infrastructure;

public static class IdentityModelConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityUser>(builder =>
        {
            builder.ToTable("iam_users", "iam");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SubjectId).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
            builder.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.Property(x => x.ClientId);
            builder.Property(x => x.ContactPersonId);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();

            builder.HasIndex(x => x.SubjectId).IsUnique();
            builder.HasIndex(x => x.NormalizedEmail).IsUnique();
            builder.HasIndex(x => x.ClientId);
            builder.HasIndex(x => x.ContactPersonId);
        });

        modelBuilder.Entity<IdentityRefreshToken>(builder =>
        {
            builder.ToTable("iam_refresh_tokens", "iam");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ExpiresAtUtc).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.RevokedAtUtc);
            builder.Property(x => x.ReplacedByTokenId);

            builder.HasIndex(x => x.TokenHash).IsUnique();
            builder.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<IdentityRole>(builder =>
        {
            builder.ToTable("iam_roles", "iam");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.IsSystem).IsRequired();

            builder.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<IdentityPermission>(builder =>
        {
            builder.ToTable("iam_permissions", "iam");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).HasMaxLength(128).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();

            builder.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<UserRoleAssignment>(builder =>
        {
            builder.ToTable("iam_role_assignments", "iam");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ScopeType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.ScopeId).HasMaxLength(64);
            builder.Property(x => x.AssignedAtUtc).IsRequired();

            builder.HasIndex(x => new { x.UserId, x.RoleId, x.ScopeType, x.ScopeId }).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(builder =>
        {
            builder.ToTable("iam_role_permissions", "iam");
            builder.HasKey(x => new { x.RoleId, x.PermissionId });
        });
    }
}
