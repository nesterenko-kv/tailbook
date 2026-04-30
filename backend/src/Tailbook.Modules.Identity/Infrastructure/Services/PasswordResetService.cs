using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.Modules.Identity.Contracts;
using Tailbook.Modules.Identity.Infrastructure.Options;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed class PasswordResetService(
    AppDbContext dbContext,
    PasswordHasher passwordHasher,
    IAuditTrailService auditTrailService,
    IOptions<PasswordResetOptions> optionsAccessor) : IPasswordResetService
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
        var utcNow = DateTime.UtcNow;
        var expiresAtUtc = utcNow.AddMinutes(options.ExpirationMinutes);

        dbContext.Set<IdentityPasswordResetToken>().Add(new IdentityPasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = RefreshTokenService.Hash(rawToken),
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = utcNow
        });

        dbContext.Set<OutboxMessage>().Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            ModuleCode = ModuleCode,
            EventType = PasswordResetRequestedEventType,
            PayloadJson = JsonSerializer.Serialize(new PasswordResetRequestedPayload(user.Email, user.DisplayName, rawToken, expiresAtUtc)),
            OccurredAtUtc = utcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync(
            ModuleCode,
            "iam_user",
            user.Id.ToString("D"),
            "PASSWORD_RESET_REQUESTED",
            null,
            null,
            JsonSerializer.Serialize(new { expiresAtUtc }),
            cancellationToken);
    }

    public async Task<PasswordResetResult> ResetPasswordAsync(string rawToken, string newPassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return PasswordResetResult.InvalidToken;
        }

        var tokenHash = RefreshTokenService.Hash(rawToken);
        var resetToken = await dbContext.Set<IdentityPasswordResetToken>()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (resetToken is null)
        {
            return PasswordResetResult.InvalidToken;
        }

        if (resetToken.UsedAtUtc is not null)
        {
            return PasswordResetResult.TokenAlreadyUsed;
        }

        var utcNow = DateTime.UtcNow;
        if (resetToken.ExpiresAtUtc <= utcNow)
        {
            return PasswordResetResult.TokenExpired;
        }

        var user = await dbContext.Set<IdentityUser>()
            .SingleOrDefaultAsync(x => x.Id == resetToken.UserId && x.Status == UserStatusCodes.Active, cancellationToken);
        if (user is null)
        {
            return PasswordResetResult.InvalidToken;
        }

        user.PasswordHash = passwordHasher.Hash(newPassword);
        user.UpdatedAtUtc = utcNow;
        resetToken.UsedAtUtc = utcNow;

        var activeRefreshTokens = await dbContext.Set<IdentityRefreshToken>()
            .Where(x => x.UserId == user.Id && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.RevokedAtUtc = utcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync(
            ModuleCode,
            "iam_user",
            user.Id.ToString("D"),
            "PASSWORD_RESET_COMPLETED",
            null,
            null,
            JsonSerializer.Serialize(new { resetToken.Id, resetToken.UsedAtUtc }),
            cancellationToken);
        return PasswordResetResult.Success;
    }

    private static string GenerateToken(int byteCount)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed record PasswordResetRequestedPayload(string Email, string DisplayName, string ResetToken, DateTime ExpiresAtUtc);
}
