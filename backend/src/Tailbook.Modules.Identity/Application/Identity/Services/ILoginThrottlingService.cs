namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface ILoginThrottlingService
{
    LoginThrottleDecision CheckAllowed(string email);
    void RecordFailure(string email);
    void RecordSuccess(string email);
}
