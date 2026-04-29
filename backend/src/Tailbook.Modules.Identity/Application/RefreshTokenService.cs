using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Domain;
using Tailbook.Modules.Identity.Infrastructure;

namespace Tailbook.Modules.Identity.Application;

public sealed class RefreshTokenService(AppDbContext dbContext, IOptions<RefreshTokenOptions> optionsAccessor)
{
    public async Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        var options = optionsAccessor.Value;
        var rawToken = GenerateToken(options.TokenBytes);
        var utcNow = DateTime.UtcNow;

        var entity = new IdentityRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAtUtc = utcNow.AddDays(options.ExpirationDays),
            CreatedAtUtc = utcNow
        };

        dbContext.Set<IdentityRefreshToken>().Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new IssuedRefreshToken(rawToken, entity.ExpiresAtUtc);
    }

    public async Task<IdentityRefreshToken?> FindUsableAsync(string rawToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var tokenHash = Hash(rawToken);
        var utcNow = DateTime.UtcNow;

        return await dbContext.Set<IdentityRefreshToken>()
            .SingleOrDefaultAsync(x =>
                x.TokenHash == tokenHash
                && x.RevokedAtUtc == null
                && x.ExpiresAtUtc > utcNow,
                cancellationToken);
    }

    public async Task<IssuedRefreshToken> RotateAsync(IdentityRefreshToken existingToken, CancellationToken cancellationToken)
    {
        var options = optionsAccessor.Value;
        var rawToken = GenerateToken(options.TokenBytes);
        var utcNow = DateTime.UtcNow;
        var replacement = new IdentityRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existingToken.UserId,
            TokenHash = Hash(rawToken),
            ExpiresAtUtc = utcNow.AddDays(options.ExpirationDays),
            CreatedAtUtc = utcNow
        };

        existingToken.RevokedAtUtc = utcNow;
        existingToken.ReplacedByTokenId = replacement.Id;
        dbContext.Set<IdentityRefreshToken>().Add(replacement);

        await dbContext.SaveChangesAsync(cancellationToken);
        return new IssuedRefreshToken(rawToken, replacement.ExpiresAtUtc);
    }

    public async Task<bool> RevokeAsync(string rawToken, CancellationToken cancellationToken)
    {
        var token = await FindUsableAsync(rawToken, cancellationToken);
        if (token is null)
        {
            return false;
        }

        token.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public static string Hash(string rawToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(rawToken);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToBase64String(hashBytes);
    }

    private static string GenerateToken(int byteCount)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

public sealed record IssuedRefreshToken(string Token, DateTime ExpiresAtUtc);
