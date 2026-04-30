namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IMfaFactorService
{
    Task<IReadOnlyCollection<MfaFactorView>> ListFactorsAsync(Guid userId, CancellationToken cancellationToken);
    Task<MfaFactorView?> EnableEmailOtpAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> DisableFactorAsync(Guid userId, Guid factorId, CancellationToken cancellationToken);
}
