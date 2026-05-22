namespace Tailbook.Modules.Identity.Application.Identity.Models;

public readonly record struct LoginThrottleDecision(bool IsLockedOut, TimeSpan? RetryAfter)
{
    public static LoginThrottleDecision Allowed => new(false, null);

    public static LoginThrottleDecision Locked(TimeSpan retryAfter) => new(true, retryAfter);
}
