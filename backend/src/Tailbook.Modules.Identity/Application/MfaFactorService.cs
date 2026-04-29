using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Contracts;
using Tailbook.Modules.Identity.Domain;

namespace Tailbook.Modules.Identity.Application;

public sealed class MfaFactorService(AppDbContext dbContext)
{
    public async Task<IReadOnlyCollection<MfaFactorView>> ListFactorsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<IdentityMfaFactor>()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.FactorType)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new MfaFactorView(x.Id, x.FactorType, x.Status, x.TargetEmail, x.CreatedAtUtc, x.EnabledAtUtc, x.DisabledAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<MfaFactorView?> EnableEmailOtpAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<IdentityUser>()
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var utcNow = DateTime.UtcNow;
        var factor = await dbContext.Set<IdentityMfaFactor>()
            .Where(x => x.UserId == userId && x.FactorType == MfaFactorTypes.EmailOtp)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (factor is null)
        {
            factor = new IdentityMfaFactor
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FactorType = MfaFactorTypes.EmailOtp,
                CreatedAtUtc = utcNow
            };
            dbContext.Set<IdentityMfaFactor>().Add(factor);
        }

        factor.Status = MfaFactorStatusCodes.Enabled;
        factor.TargetEmail = user.Email;
        factor.EnabledAtUtc = utcNow;
        factor.DisabledAtUtc = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToView(factor);
    }

    public async Task<bool> DisableFactorAsync(Guid userId, Guid factorId, CancellationToken cancellationToken)
    {
        var factor = await dbContext.Set<IdentityMfaFactor>()
            .SingleOrDefaultAsync(x => x.Id == factorId && x.UserId == userId, cancellationToken);
        if (factor is null)
        {
            return false;
        }

        if (factor.Status != MfaFactorStatusCodes.Disabled)
        {
            factor.Status = MfaFactorStatusCodes.Disabled;
            factor.DisabledAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private static MfaFactorView ToView(IdentityMfaFactor factor)
    {
        return new MfaFactorView(factor.Id, factor.FactorType, factor.Status, factor.TargetEmail, factor.CreatedAtUtc, factor.EnabledAtUtc, factor.DisabledAtUtc);
    }
}

public sealed record MfaFactorView(
    Guid Id,
    string FactorType,
    string Status,
    string TargetEmail,
    DateTime CreatedAtUtc,
    DateTime? EnabledAtUtc,
    DateTime? DisabledAtUtc);
