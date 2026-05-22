namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface ILoginThrottlingService
{
    ValueTask<LoginThrottleDecision> CheckAllowedAsync(string email, CancellationToken cancellationToken = default);
    ValueTask RecordFailureAsync(string email, CancellationToken cancellationToken = default);
    ValueTask RecordSuccessAsync(string email, CancellationToken cancellationToken = default);
}
