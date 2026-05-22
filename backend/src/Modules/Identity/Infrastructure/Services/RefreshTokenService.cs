using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Infrastructure;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Infrastructure.Options;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed class RefreshTokenService(
    AppDbContext dbContext,
    IDistributedCache cache,
    IOptions<RefreshTokenOptions> optionsAccessor,
    TimeProvider timeProvider)
{
    public async Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        var options = optionsAccessor.Value;
        var rawToken = GenerateToken(options.TokenBytes);
        var utcNow = timeProvider.GetUtcNow();

        var entity = new IdentityRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAt = utcNow.AddDays(options.ExpirationDays),
            CreatedAt = utcNow
        };

        dbContext.Set<IdentityRefreshToken>().Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new IssuedRefreshToken(rawToken, entity.ExpiresAt);
    }

    public async ValueTask<IdentityRefreshToken?> FindUsableAsync(string rawToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var tokenHash = Hash(rawToken);

        var blacklisted = await cache.GetStringAsync(CacheKeys.RefreshTokenBlacklist(tokenHash), cancellationToken);
        if (blacklisted is not null)
        {
            return null;
        }

        var utcNow = timeProvider.GetUtcNow();

        return await dbContext.Set<IdentityRefreshToken>()
            .SingleOrDefaultAsync(x =>
                x.TokenHash == tokenHash
                && x.RevokedAt == null
                && x.ExpiresAt > utcNow,
                cancellationToken);
    }

    public async Task<IssuedRefreshToken> RotateAsync(IdentityRefreshToken existingToken, CancellationToken cancellationToken)
    {
        var options = optionsAccessor.Value;
        var rawToken = GenerateToken(options.TokenBytes);
        var utcNow = timeProvider.GetUtcNow();
        var replacement = new IdentityRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existingToken.UserId,
            TokenHash = Hash(rawToken),
            ExpiresAt = utcNow.AddDays(options.ExpirationDays),
            CreatedAt = utcNow
        };

        existingToken.RevokedAt = utcNow;
        existingToken.ReplacedByTokenId = replacement.Id;
        dbContext.Set<IdentityRefreshToken>().Add(replacement);

        await dbContext.SaveChangesAsync(cancellationToken);

        await cache.SetStringAsync(CacheKeys.RefreshTokenBlacklist(existingToken.TokenHash), "revoked", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(options.ExpirationDays)
        }, cancellationToken);

        return new IssuedRefreshToken(rawToken, replacement.ExpiresAt);
    }

    public async Task<bool> RevokeAsync(string rawToken, CancellationToken cancellationToken)
    {
        var token = await FindUsableAsync(rawToken, cancellationToken);
        if (token is null)
        {
            return false;
        }

        token.RevokedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);

        var remaining = token.ExpiresAt - timeProvider.GetUtcNow();
        if (remaining > TimeSpan.Zero)
        {
            await cache.SetStringAsync(CacheKeys.RefreshTokenBlacklist(token.TokenHash), "revoked", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = remaining
            }, cancellationToken);
        }

        return true;
    }

    public static string Hash(string rawToken)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken), hash);
        return Convert.ToBase64String(hash);
    }

    private static string GenerateToken(int byteCount)
    {
        Span<byte> bytes = stackalloc byte[byteCount];
        RandomNumberGenerator.Fill(bytes);

        Span<char> base64 = stackalloc char[((byteCount + 2) / 3) * 4];
        Convert.TryToBase64Chars(bytes, base64, out var written);

        var trimmed = base64[..written].TrimEnd('=').ToArray();
        for (var i = 0; i < trimmed.Length; i++)
        {
            if (trimmed[i] == '+') trimmed[i] = '-';
            else if (trimmed[i] == '/') trimmed[i] = '_';
        }

        return new string(trimmed);
    }
}
