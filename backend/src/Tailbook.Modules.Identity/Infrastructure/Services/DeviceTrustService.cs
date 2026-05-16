using System.Security.Cryptography;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Application.Identity.Services;
using Tailbook.Modules.Identity.Domain.Entities;
using Tailbook.Modules.Identity.Infrastructure.Options;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed class DeviceTrustService(
    AppDbContext dbContext,
    IOptions<DeviceTrustOptions> options,
    TimeProvider timeProvider) : IDeviceTrustService
{
    public async Task<ErrorOr<string>> IssueTrustTokenAsync(Guid userId, string surface, string? label, CancellationToken cancellationToken)
    {
        var rawToken = GenerateToken(options.Value.TokenBytes);
        var tokenHash = HashToken(rawToken);
        var utcNow = timeProvider.GetUtcNow();

        dbContext.Set<IdentityDeviceTrust>().Add(new IdentityDeviceTrust
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceTokenHash = tokenHash,
            Surface = surface,
            Label = label,
            CreatedAt = utcNow,
            ExpiresAt = utcNow.AddDays(options.Value.DurationDays),
            LastUsedAt = utcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return rawToken;
    }

    public async ValueTask<bool> IsDeviceTrustedAsync(string deviceToken, string surface, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(deviceToken))
        {
            return false;
        }

        var tokenHash = HashToken(deviceToken);
        var utcNow = timeProvider.GetUtcNow();

        var trust = await dbContext.Set<IdentityDeviceTrust>()
            .Where(x => x.DeviceTokenHash == tokenHash && x.Surface == surface && x.ExpiresAt > utcNow)
            .FirstOrDefaultAsync(cancellationToken);

        if (trust is null)
        {
            return false;
        }

        trust.LastUsedAt = utcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<DeviceTrustView>> ListTrustsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<IdentityDeviceTrust>()
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ExpiresAt > timeProvider.GetUtcNow())
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new DeviceTrustView(x.Id, x.Surface, x.Label, x.CreatedAt, x.ExpiresAt, x.LastUsedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ErrorOr<Success>> RevokeTrustAsync(Guid trustId, Guid userId, CancellationToken cancellationToken)
    {
        var trust = await dbContext.Set<IdentityDeviceTrust>()
            .Where(x => x.Id == trustId && x.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (trust is null)
        {
            return Error.NotFound("Identity.DeviceTrustNotFound", "Device trust entry not found.");
        }

        dbContext.Set<IdentityDeviceTrust>().Remove(trust);
        await dbContext.SaveChangesAsync(cancellationToken);
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

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
