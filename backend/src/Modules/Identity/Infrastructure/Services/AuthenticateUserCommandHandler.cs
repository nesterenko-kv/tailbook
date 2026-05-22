using System.Data;
using System.Data.Common;
using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Infrastructure.Persistence;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public class AuthenticateUserCommandHandler(
    AppDbContext dbContext,
    PasswordHasher passwordHasher,
    IdentitySessionService identitySessionService,
    IMfaChallengeService mfaChallengeService,
    IDeviceTrustService deviceTrustService
) : ICommandHandler<AuthenticateUserCommand, ErrorOr<AuthenticationResult>>, IAuthenticateUserService
{
    public Task<ErrorOr<AuthenticationResult>> ExecuteAsync(AuthenticateUserCommand command, CancellationToken cancellationToken)
    {
        return AuthenticateAsync(
            command.Email,
            command.Password,
            requireClientPortalAccess: false,
            enforceMfa: false,
            requestIpAddress: null,
            userAgent: null,
            deviceTrustToken: null,
            cancellationToken);
    }

    public async Task<ErrorOr<AuthenticationResult>> AuthenticateAsync(
        string email,
        string password,
        bool requireClientPortalAccess,
        bool enforceMfa,
        string? requestIpAddress,
        string? userAgent,
        string? deviceTrustToken,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = IdentityUseCases.NormalizeEmail(email);

        // Use raw SQL for PostgreSQL (single round-trip) or EF Core queries for InMemory/testing
        UserAuthData? userAuth;
        if (dbContext.Database.ProviderName != null
            && dbContext.Database.ProviderName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            userAuth = await LoadUserAuthAsync(normalizedEmail, cancellationToken);
        }
        else
        {
            userAuth = await LoadUserAuthEfAsync(normalizedEmail, cancellationToken);
        }

        if (userAuth is null || !string.Equals(userAuth.Status, UserStatusCodes.Active, StringComparison.OrdinalIgnoreCase))
        {
            return IdentityErrors.InvalidCredentials();
        }

        if (!passwordHasher.Verify(password, userAuth.PasswordHash))
        {
            return IdentityErrors.InvalidCredentials();
        }

        if (requireClientPortalAccess && (userAuth.ClientId is null || userAuth.ContactPersonId is null))
        {
            return IdentityErrors.ClientPortalAccessRequired();
        }

        if (requireClientPortalAccess && !userAuth.PermissionCodes.Contains(PermissionCodes.ClientPortalAccess, StringComparer.OrdinalIgnoreCase))
        {
            return IdentityErrors.ClientPortalAccessRequired();
        }

        if (enforceMfa && userAuth.HasMfaFactor)
        {
            if (!string.IsNullOrWhiteSpace(deviceTrustToken) && await deviceTrustService.IsDeviceTrustedAsync(deviceTrustToken, "admin", cancellationToken))
            {
                var user = BuildUserFromAuth(userAuth);
                var bypassLogin = await identitySessionService.CreateSessionAsync(user, userAuth.RoleCodes, userAuth.PermissionCodes, cancellationToken);
                return new AuthenticationSucceededResult(bypassLogin);
            }

            var challenge = await mfaChallengeService.CreateEmailOtpChallengeAsync(userAuth.Id, requestIpAddress, userAgent, cancellationToken);
            if (challenge.IsError)
            {
                return challenge.Errors;
            }

            return new AuthenticationMfaRequiredResult(challenge.Value.ChallengeId, challenge.Value.FactorType, challenge.Value.ExpiresAt);
        }

        var userEntity = BuildUserFromAuth(userAuth);
        var login = await identitySessionService.CreateSessionAsync(userEntity, userAuth.RoleCodes, userAuth.PermissionCodes, cancellationToken);
        return new AuthenticationSucceededResult(login);
    }

    private async Task<UserAuthData?> LoadUserAuthEfAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<IdentityUser>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await GetRoleCodesAsync(user.Id, cancellationToken);
        var permissions = await GetPermissionCodesAsync(user.Id, cancellationToken);
        var hasMfa = await HasEnabledEmailOtpFactorAsync(user.Id, cancellationToken);

        return new UserAuthData(
            user.Id, user.SubjectId, user.Email, user.NormalizedEmail,
            user.DisplayName, user.PasswordHash, user.Status,
            user.ClientId, user.ContactPersonId,
            user.CreatedAt, user.UpdatedAt,
            roles, permissions, hasMfa
        );
    }

    private async Task<bool> HasEnabledEmailOtpFactorAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<IdentityMfaFactor>()
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId
                           && x.FactorType == MfaFactorTypes.EmailOtp
                           && x.Status == MfaFactorStatusCodes.Enabled,
                cancellationToken);
    }

    private async Task<HashSet<string>> GetRoleCodesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserRoleAssignment>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Join(
                dbContext.Set<IdentityRole>().AsNoTracking(),
                assignment => assignment.RoleId,
                role => role.Id,
                (_, role) => role.Code)
            .Distinct()
            .OrderBy(x => x)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    private async Task<HashSet<string>> GetPermissionCodesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserRoleAssignment>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Join(
                dbContext.Set<RolePermission>().AsNoTracking(),
                assignment => assignment.RoleId,
                rolePermission => rolePermission.RoleId,
                (_, rolePermission) => rolePermission.PermissionId)
            .Join(
                dbContext.Set<IdentityPermission>().AsNoTracking(),
                permissionId => permissionId,
                permission => permission.Id,
                (_, permission) => permission.Code)
            .Distinct()
            .OrderBy(x => x)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken: cancellationToken);
    }

    private static IdentityUser BuildUserFromAuth(UserAuthData auth)
    {
        return new IdentityUser
        {
            Id = auth.Id,
            SubjectId = auth.SubjectId,
            Email = auth.Email,
            NormalizedEmail = auth.NormalizedEmail,
            DisplayName = auth.DisplayName,
            PasswordHash = auth.PasswordHash,
            Status = auth.Status,
            ClientId = auth.ClientId,
            ContactPersonId = auth.ContactPersonId,
            CreatedAt = auth.CreatedAt,
            UpdatedAt = auth.UpdatedAt
        };
    }

    private async Task<UserAuthData?> LoadUserAuthAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = AuthenticateSql.LoginQuery;

        var param = cmd.CreateParameter();
        param.ParameterName = "normalizedEmail";
        param.Value = normalizedEmail;
        param.DbType = DbType.String;
        cmd.Parameters.Add(param);

        // Keep connection open — same connection reused by EF for refresh token write
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadUserAuth(reader);
    }

    private static UserAuthData ReadUserAuth(DbDataReader reader)
    {
        return new UserAuthData(
            Id: reader.GetGuid(0),
            SubjectId: reader.GetString(1),
            Email: reader.GetString(2),
            NormalizedEmail: reader.GetString(3),
            DisplayName: reader.GetString(4),
            PasswordHash: reader.GetString(5),
            Status: reader.GetString(6),
            ClientId: reader.IsDBNull(7) ? null : reader.GetGuid(7),
            ContactPersonId: reader.IsDBNull(8) ? null : reader.GetGuid(8),
            CreatedAt: reader.GetFieldValue<DateTimeOffset>(9),
            UpdatedAt: reader.GetFieldValue<DateTimeOffset>(10),
            RoleCodes: reader.IsDBNull(11) ? [] : reader.GetFieldValue<string[]>(11),
            PermissionCodes: reader.IsDBNull(12) ? [] : reader.GetFieldValue<string[]>(12),
            HasMfaFactor: reader.GetBoolean(13)
        );
    }

    private sealed record UserAuthData(
        Guid Id,
        string SubjectId,
        string Email,
        string NormalizedEmail,
        string DisplayName,
        string PasswordHash,
        string Status,
        Guid? ClientId,
        Guid? ContactPersonId,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        IReadOnlyCollection<string> RoleCodes,
        IReadOnlyCollection<string> PermissionCodes,
        bool HasMfaFactor
    );
}
