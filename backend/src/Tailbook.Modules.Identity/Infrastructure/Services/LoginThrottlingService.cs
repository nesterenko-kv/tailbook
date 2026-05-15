using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Tailbook.Modules.Identity.Infrastructure.Options;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed class LoginThrottlingService(IOptions<LoginThrottlingOptions> optionsAccessor, TimeProvider timeProvider) : ILoginThrottlingService
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

        var now = timeProvider.GetUtcNow();
        lock (state)
        {
            if (state.LockoutUntil is null)
            {
                return LoginThrottleDecision.Allowed;
            }

            if (state.LockoutUntil > now)
            {
                return LoginThrottleDecision.Locked(state.LockoutUntil.Value - now);
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

        var now = timeProvider.GetUtcNow();
        var options = optionsAccessor.Value;
        var failureWindow = TimeSpan.FromMinutes(options.FailureWindowMinutes);
        var lockout = TimeSpan.FromMinutes(options.LockoutMinutes);
        var state = _attempts.GetOrAdd(key, static (_, v) => new LoginAttemptState(v), now);

        lock (state)
        {
            if (state.LockoutUntil is not null && state.LockoutUntil > now)
            {
                return;
            }

            if (now - state.FirstFailure > failureWindow)
            {
                state.FirstFailure = now;
                state.FailedAttempts = 0;
                state.LockoutUntil = null;
            }

            state.FailedAttempts++;
            if (state.FailedAttempts >= options.MaxFailedAttempts)
            {
                state.LockoutUntil = now.Add(lockout);
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

    private static string NormalizeKey(string email) => IdentityUseCases.NormalizeEmail(email);

    private sealed class LoginAttemptState(DateTimeOffset firstFailure)
    {
        public DateTimeOffset FirstFailure { get; set; } = firstFailure;
        public int FailedAttempts { get; set; }
        public DateTimeOffset? LockoutUntil { get; set; }
    }
}
