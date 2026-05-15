using ErrorOr;

namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IMfaFactorService
{
    Task<IReadOnlyCollection<MfaFactorView>> ListFactorsAsync(Guid userId, CancellationToken cancellationToken);
    Task<ErrorOr<MfaFactorView>> EnableEmailOtpAsync(Guid userId, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> DisableFactorAsync(Guid userId, Guid factorId, CancellationToken cancellationToken);
}
