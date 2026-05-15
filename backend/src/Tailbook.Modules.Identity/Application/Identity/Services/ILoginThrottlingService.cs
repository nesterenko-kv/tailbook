namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface ILoginThrottlingService
{
    Task<LoginThrottleDecision> CheckAllowedAsync(string email, CancellationToken cancellationToken = default);
    Task RecordFailureAsync(string email, CancellationToken cancellationToken = default);
    Task RecordSuccessAsync(string email, CancellationToken cancellationToken = default);
}
