using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Contracts;
using Tailbook.Modules.Identity.Domain;
using Tailbook.Modules.Identity.Infrastructure;

namespace Tailbook.Modules.Identity.Application;

public sealed class IdentitySeeder(IOptions<BootstrapAdminOptions> bootstrapAdminOptions, PasswordHasher passwordHasher) : IDataSeeder
{
    public int Order => 10;

    public async Task SeedAsync(AppDbContext dbContext, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var existingPermissions = await dbContext.Set<IdentityPermission>()
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var permission in SystemRoleCatalog.Permissions)
        {
            if (existingPermissions.ContainsKey(permission.Code))
            {
                continue;
            }

            dbContext.Set<IdentityPermission>().Add(new IdentityPermission
            {
                Id = Guid.NewGuid(),
                Code = permission.Code,
                DisplayName = permission.DisplayName
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var permissions = await dbContext.Set<IdentityPermission>()
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var existingRoles = await dbContext.Set<IdentityRole>()
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var role in SystemRoleCatalog.Roles)
        {
            if (existingRoles.ContainsKey(role.Code))
            {
                continue;
            }

            var entity = new IdentityRole
            {
                Id = Guid.NewGuid(),
                Code = role.Code,
                DisplayName = role.DisplayName,
                IsSystem = true
            };

            dbContext.Set<IdentityRole>().Add(entity);
            existingRoles[role.Code] = entity;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var rolePermissions = await dbContext.Set<RolePermission>().ToListAsync(cancellationToken);
        foreach (var role in SystemRoleCatalog.Roles)
        {
            var roleEntity = existingRoles[role.Code];
            foreach (var permissionCode in role.PermissionCodes)
            {
                var permissionEntity = permissions[permissionCode];
                var exists = rolePermissions.Any(x => x.RoleId == roleEntity.Id && x.PermissionId == permissionEntity.Id);
                if (exists)
                {
                    continue;
                }

                dbContext.Set<RolePermission>().Add(new RolePermission
                {
                    RoleId = roleEntity.Id,
                    PermissionId = permissionEntity.Id
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var options = bootstrapAdminOptions.Value;
        var normalizedEmail = IdentityQueries.NormalizeEmail(options.Email);
        var adminUser = await dbContext.Set<IdentityUser>()
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

        if (adminUser is null)
        {
            adminUser = new IdentityUser
            {
                Id = Guid.NewGuid(),
                SubjectId = $"usr_{Guid.NewGuid():N}",
                Email = options.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                DisplayName = options.DisplayName.Trim(),
                PasswordHash = passwordHasher.Hash(options.Password),
                Status = UserStatusCodes.Active,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            dbContext.Set<IdentityUser>().Add(adminUser);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var adminRole = existingRoles[RoleCodes.Admin];
        var hasAdminAssignment = await dbContext.Set<UserRoleAssignment>()
            .AnyAsync(x => x.UserId == adminUser.Id && x.RoleId == adminRole.Id && x.ScopeType == "Global" && x.ScopeId == null, cancellationToken);

        if (!hasAdminAssignment)
        {
            dbContext.Set<UserRoleAssignment>().Add(new UserRoleAssignment
            {
                Id = Guid.NewGuid(),
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                ScopeType = "Global",
                ScopeId = null,
                AssignedAtUtc = DateTime.UtcNow,
                AssignedByUserId = adminUser.Id
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
