using System.Security.Cryptography;
using System.Text.Json;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Abstractions.Security;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.Modules.Identity.Infrastructure.Options;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed class PasswordResetService(
    AppDbContext dbContext,
    PasswordHasher passwordHasher,
    IAuditTrailService auditTrailService,
    ISensitivePayloadProtector sensitivePayloadProtector,
    IOptions<PasswordResetOptions> optionsAccessor,
    TimeProvider timeProvider) : IPasswordResetService
{
    private const string ModuleCode = "identity";
    private const string PasswordResetRequestedEventType = "Tailbook.Modules.Identity.Integration.PasswordResetRequested";

    public async Task RequestResetAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = IdentityUseCases.NormalizeEmail(email);
        var user = await dbContext.Set<IdentityUser>()
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail && x.Status == UserStatusCodes.Active, cancellationToken);

        if (user is null)
        {
            return;
        }

        var options = optionsAccessor.Value;
        var rawToken = GenerateToken(options.TokenBytes);
        var utcNow = timeProvider.GetUtcNow();
        var expiresAt = utcNow.AddMinutes(options.ExpirationMinutes);
        var resetLink = BuildResetLink(options.ResetUrlBase, rawToken);
        var protectedResetLink = sensitivePayloadProtector.Protect(SensitivePayloadPurposes.PasswordResetLink, resetLink);

        dbContext.Set<IdentityPasswordResetToken>().Add(new IdentityPasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = RefreshTokenService.Hash(rawToken),
            ExpiresAt = expiresAt,
            CreatedAt = utcNow
        });

        dbContext.Set<OutboxMessage>().Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            ModuleCode = ModuleCode,
            EventType = PasswordResetRequestedEventType,
            PayloadJson = JsonSerializer.Serialize(new PasswordResetRequestedPayload(user.Email, user.DisplayName, protectedResetLink, expiresAt)),
            OccurredAt = utcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync(
            ModuleCode,
            "iam_user",
            user.Id.ToString("D"),
            "PASSWORD_RESET_REQUESTED",
            null,
            null,
            JsonSerializer.Serialize(new { expiresAt }),
            cancellationToken);
    }

    public async Task<ErrorOr<Success>> ResetPasswordAsync(string rawToken, string newPassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return IdentityErrors.InvalidPasswordResetToken();
        }

        var tokenHash = RefreshTokenService.Hash(rawToken);
        var resetToken = await dbContext.Set<IdentityPasswordResetToken>()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (resetToken is null)
        {
            return IdentityErrors.InvalidPasswordResetToken();
        }

        if (resetToken.UsedAt is not null)
        {
            return IdentityErrors.UsedPasswordResetToken();
        }

        var utcNow = timeProvider.GetUtcNow();
        if (resetToken.ExpiresAt <= utcNow)
        {
            return IdentityErrors.ExpiredPasswordResetToken();
        }

        var user = await dbContext.Set<IdentityUser>()
            .SingleOrDefaultAsync(x => x.Id == resetToken.UserId && x.Status == UserStatusCodes.Active, cancellationToken);
        if (user is null)
        {
            return IdentityErrors.InvalidPasswordResetToken();
        }

        user.PasswordHash = passwordHasher.Hash(newPassword);
        user.UpdatedAt = utcNow;
        resetToken.UsedAt = utcNow;

        var activeRefreshTokens = await dbContext.Set<IdentityRefreshToken>()
            .Where(x => x.UserId == user.Id && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.RevokedAt = utcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync(
            ModuleCode,
            "iam_user",
            user.Id.ToString("D"),
            "PASSWORD_RESET_COMPLETED",
            null,
            null,
            JsonSerializer.Serialize(new { resetToken.Id, resetToken.UsedAt }),
            cancellationToken);
        return Result.Success;
    }

    private static string GenerateToken(int byteCount)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string BuildResetLink(string resetUrlBase, string rawToken)
    {
        var builder = new UriBuilder(resetUrlBase);
        var query = builder.Query.TrimStart('?');
        var tokenParameter = "token=" + Uri.EscapeDataString(rawToken);
        builder.Query = string.IsNullOrWhiteSpace(query) ? tokenParameter : query + "&" + tokenParameter;
        return builder.Uri.ToString();
    }

    private sealed record PasswordResetRequestedPayload(string Email, string DisplayName, string ProtectedResetLink, DateTimeOffset ExpiresAt);
}
