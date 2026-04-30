using System.Text.Json;
using System.Security.Claims;
using ErrorOr;
using FastEndpoints.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Contracts;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed class IdentityUseCases(AppDbContext dbContext, PasswordHasher passwordHasher, IAuditTrailService auditTrailService) : IIdentityReadService
{
    public async Task<IReadOnlyList<RoleView>> ListRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await dbContext.Set<IdentityRole>()
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

        var permissions = await dbContext.Set<RolePermission>()
            .Join(
                dbContext.Set<IdentityPermission>(),
                rp => rp.PermissionId,
                permission => permission.Id,
                (rp, permission) => new { rp.RoleId, permission.Code })
            .ToListAsync(cancellationToken);

        return roles
            .Select(role => new RoleView(
                role.Id,
                role.Code,
                role.DisplayName,
                permissions.Where(x => x.RoleId == role.Id).Select(x => x.Code).OrderBy(x => x).ToArray()))
            .ToArray();
    }

    public async Task<IReadOnlyList<PermissionView>> ListPermissionsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Set<IdentityPermission>()
            .OrderBy(x => x.Code)
            .Select(x => new PermissionView(x.Id, x.Code, x.DisplayName))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<UserSummaryView>> ListUsersAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize switch
        {
            <= 0 => 20,
            > 100 => 100,
            _ => pageSize
        };

        var totalCount = await dbContext.Set<IdentityUser>().CountAsync(cancellationToken);
        var users = await dbContext.Set<IdentityUser>()
            .OrderBy(x => x.Email)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        var roleMap = await LoadRoleMapAsync(users.Select(x => x.Id).ToArray(), cancellationToken);

        var items = users
            .Select(user => new UserSummaryView(
                user.Id,
                user.SubjectId,
                user.Email,
                user.DisplayName,
                user.Status,
                roleMap.GetValueOrDefault(user.Id, []).OrderBy(x => x).ToArray(),
                user.CreatedAtUtc,
                user.UpdatedAtUtc))
            .ToArray();

        return new PagedResult<UserSummaryView>(items, safePage, safePageSize, totalCount);
    }

    public async Task<UserDetailView?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<IdentityUser>().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roles = await GetRoleCodesAsync(user.Id, cancellationToken);
        var permissions = await GetPermissionCodesAsync(user.Id, cancellationToken);

        return new UserDetailView(
            user.Id,
            user.SubjectId,
            user.Email,
            user.DisplayName,
            user.Status,
            roles,
            permissions,
            user.CreatedAtUtc,
            user.UpdatedAtUtc);
    }

    public async Task<ErrorOr<UserDetailView>> CreateUserAsync(string email, string displayName, string password, IReadOnlyCollection<string> roleCodes, Guid? assignedByUserId, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var exists = await dbContext.Set<IdentityUser>().AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (exists)
        {
            return Error.Conflict("Identity.UserEmailExists", $"User with email '{email}' already exists.");
        }

        var utcNow = DateTime.UtcNow;
        var user = new IdentityUser
        {
            Id = Guid.NewGuid(),
            SubjectId = $"usr_{Guid.NewGuid():N}",
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHasher.Hash(password),
            Status = UserStatusCodes.Active,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        dbContext.Set<IdentityUser>().Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync(
            "identity",
            "iam_user",
            user.Id.ToString("D"),
            "CREATE_USER",
            assignedByUserId,
            null,
            JsonSerializer.Serialize(new { user.Email, user.DisplayName, user.Status }),
            cancellationToken);

        if (roleCodes.Count > 0)
        {
            var assignmentResult = await AssignRolesAsync(user.Id, roleCodes, assignedByUserId, cancellationToken);
            if (assignmentResult.IsError)
            {
                return assignmentResult.Errors;
            }
        }

        return (await GetUserAsync(user.Id, cancellationToken))!;
    }

    public async Task<ErrorOr<UserDetailView>> AssignRolesAsync(Guid userId, IReadOnlyCollection<string> roleCodes, Guid? assignedByUserId, CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Set<IdentityUser>().AnyAsync(x => x.Id == userId, cancellationToken);
        if (!userExists)
        {
            return Error.NotFound("Identity.UserNotFound", "User does not exist.");
        }

        var requestedRoleCodes = roleCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var roles = await dbContext.Set<IdentityRole>()
            .Where(x => requestedRoleCodes.Contains(x.Code))
            .ToListAsync(cancellationToken);

        if (roles.Count != requestedRoleCodes.Length)
        {
            var missing = requestedRoleCodes.Except(roles.Select(x => x.Code), StringComparer.OrdinalIgnoreCase).ToArray();
            return Error.Validation("Identity.UnknownRoles", $"Unknown roles: {string.Join(", ", missing)}");
        }

        var existingAssignments = await dbContext.Set<UserRoleAssignment>()
            .Where(x => x.UserId == userId && x.ScopeType == "Global" && x.ScopeId == null)
            .ToListAsync(cancellationToken);
        var existingRoleIds = existingAssignments.Select(x => x.RoleId).ToArray();
        var existingRoleCodes = await dbContext.Set<IdentityRole>()
            .Where(x => existingRoleIds.Contains(x.Id))
            .Select(x => x.Code)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        dbContext.Set<UserRoleAssignment>().RemoveRange(existingAssignments);
        dbContext.Set<UserRoleAssignment>().AddRange(roles.Select(role => new UserRoleAssignment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = role.Id,
            ScopeType = "Global",
            ScopeId = null,
            AssignedAtUtc = DateTime.UtcNow,
            AssignedByUserId = assignedByUserId
        }));

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync(
            "identity",
            "iam_user",
            userId.ToString("D"),
            "ASSIGN_ROLES",
            assignedByUserId,
            JsonSerializer.Serialize(new { roleCodes = existingRoleCodes }),
            JsonSerializer.Serialize(new { roleCodes = roles.Select(x => x.Code).OrderBy(x => x).ToArray() }),
            cancellationToken);
        return (await GetUserAsync(userId, cancellationToken))!;
    }

    private async Task<Dictionary<Guid, string[]>> LoadRoleMapAsync(Guid[] userIds, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Set<UserRoleAssignment>()
            .Where(x => userIds.Contains(x.UserId))
            .Join(
                dbContext.Set<IdentityRole>(),
                assignment => assignment.RoleId,
                role => role.Id,
                (assignment, role) => new { assignment.UserId, role.Code })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private async Task<HashSet<string>> GetRoleCodesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserRoleAssignment>()
            .Where(x => x.UserId == userId)
            .Join(
                dbContext.Set<IdentityRole>(),
                assignment => assignment.RoleId,
                role => role.Id,
                (_, role) => role.Code)
            .Distinct()
            .OrderBy(x => x)
            .AsAsyncEnumerable()
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    private async Task<HashSet<string>> GetPermissionCodesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserRoleAssignment>()
            .Where(x => x.UserId == userId)
            .Join(
                dbContext.Set<RolePermission>(),
                assignment => assignment.RoleId,
                rolePermission => rolePermission.RoleId,
                (_, rolePermission) => rolePermission.PermissionId)
            .Join(
                dbContext.Set<IdentityPermission>(),
                permissionId => permissionId,
                permission => permission.Id,
                (_, permission) => permission.Code)
            .Distinct()
            .OrderBy(x => x)
            .AsAsyncEnumerable()
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken: cancellationToken);
    }

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
