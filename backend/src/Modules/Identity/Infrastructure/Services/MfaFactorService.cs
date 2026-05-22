using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed class MfaFactorService(AppDbContext dbContext, TimeProvider timeProvider) : IMfaFactorService
{
    public async Task<IReadOnlyCollection<MfaFactorView>> ListFactorsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<IdentityMfaFactor>()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.FactorType)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new MfaFactorView(x.Id, x.FactorType, x.Status, x.TargetEmail, x.CreatedAt, x.EnabledAt, x.DisabledAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ErrorOr<MfaFactorView>> EnableEmailOtpAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<IdentityUser>()
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return IdentityErrors.UserNotFound();
        }

        var utcNow = timeProvider.GetUtcNow();
        var factor = await dbContext.Set<IdentityMfaFactor>()
            .Where(x => x.UserId == userId && x.FactorType == MfaFactorTypes.EmailOtp)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (factor is null)
        {
            factor = new IdentityMfaFactor
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FactorType = MfaFactorTypes.EmailOtp,
                CreatedAt = utcNow
            };
            dbContext.Set<IdentityMfaFactor>().Add(factor);
        }

        factor.Status = MfaFactorStatusCodes.Enabled;
        factor.TargetEmail = user.Email;
        factor.EnabledAt = utcNow;
        factor.DisabledAt = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToView(factor);
    }

    public async Task<ErrorOr<Success>> DisableFactorAsync(Guid userId, Guid factorId, CancellationToken cancellationToken)
    {
        var factor = await dbContext.Set<IdentityMfaFactor>()
            .SingleOrDefaultAsync(x => x.Id == factorId && x.UserId == userId, cancellationToken);
        if (factor is null)
        {
            return IdentityErrors.MfaFactorNotFound();
        }

        if (factor.Status != MfaFactorStatusCodes.Disabled)
        {
            factor.Status = MfaFactorStatusCodes.Disabled;
            factor.DisabledAt = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success;
    }

    private static MfaFactorView ToView(IdentityMfaFactor factor)
    {
        return new MfaFactorView(factor.Id, factor.FactorType, factor.Status, factor.TargetEmail, factor.CreatedAt, factor.EnabledAt, factor.DisabledAt);
    }
}
