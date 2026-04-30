using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed class LoginThrottlingService(IOptions<LoginThrottlingOptions> optionsAccessor)
{
    private readonly ConcurrentDictionary<string, LoginAttemptState> _attempts = new(StringComparer.OrdinalIgnoreCase);

    public LoginThrottleDecision CheckAllowed(string email)
    {
        var key = NormalizeKey(email);
        if (string.IsNullOrWhiteSpace(key))
        {
            return LoginThrottleDecision.Allowed;
        }

        if (!_attempts.TryGetValue(key, out var state))
        {
            return LoginThrottleDecision.Allowed;
        }

        var now = DateTime.UtcNow;
        lock (state)
        {
            if (state.LockoutUntilUtc is null)
            {
                return LoginThrottleDecision.Allowed;
            }

            if (state.LockoutUntilUtc > now)
            {
                return LoginThrottleDecision.Locked(state.LockoutUntilUtc.Value - now);
            }

            _attempts.TryRemove(key, out _);
            return LoginThrottleDecision.Allowed;
        }
    }

    public void RecordFailure(string email)
    {
        var key = NormalizeKey(email);
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var options = optionsAccessor.Value;
        var failureWindow = TimeSpan.FromMinutes(options.FailureWindowMinutes);
        var lockout = TimeSpan.FromMinutes(options.LockoutMinutes);
        var state = _attempts.GetOrAdd(key, static (_, v) => new LoginAttemptState(v), now);

        lock (state)
        {
            if (state.LockoutUntilUtc is not null && state.LockoutUntilUtc > now)
            {
                return;
            }

            if (now - state.FirstFailureUtc > failureWindow)
            {
                state.FirstFailureUtc = now;
                state.FailedAttempts = 0;
                state.LockoutUntilUtc = null;
            }

            state.FailedAttempts++;
            if (state.FailedAttempts >= options.MaxFailedAttempts)
            {
                state.LockoutUntilUtc = now.Add(lockout);
            }
        }
    }

    public void RecordSuccess(string email)
    {
        var key = NormalizeKey(email);
        if (!string.IsNullOrWhiteSpace(key))
        {
            _attempts.TryRemove(key, out _);
        }
    }

    private static string NormalizeKey(string email) => IdentityQueries.NormalizeEmail(email);

    private sealed class LoginAttemptState(DateTime firstFailureUtc)
    {
        public DateTime FirstFailureUtc { get; set; } = firstFailureUtc;
        public int FailedAttempts { get; set; }
        public DateTime? LockoutUntilUtc { get; set; }
    }
}

public readonly record struct LoginThrottleDecision(bool IsLockedOut, TimeSpan? RetryAfter)
{
    public static LoginThrottleDecision Allowed => new(false, null);

    public static LoginThrottleDecision Locked(TimeSpan retryAfter) => new(true, retryAfter);
}
