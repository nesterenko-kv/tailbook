using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Infrastructure;
using Tailbook.Modules.Identity.Infrastructure.Options;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed class LoginThrottlingService(
    IDistributedCache cache,
    IOptions<LoginThrottlingOptions> optionsAccessor,
    TimeProvider timeProvider) : ILoginThrottlingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<LoginThrottleDecision> CheckAllowedAsync(string email, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(email);
        if (key is null)
        {
            return LoginThrottleDecision.Allowed;
        }

        var data = await cache.GetStringAsync(key, cancellationToken);
        if (data is null)
        {
            return LoginThrottleDecision.Allowed;
        }

        var state = JsonSerializer.Deserialize<LoginAttemptState>(data, JsonOptions)!;
        var now = timeProvider.GetUtcNow();

        if (state.LockoutUntil is null)
        {
            return LoginThrottleDecision.Allowed;
        }

        if (state.LockoutUntil > now)
        {
            return LoginThrottleDecision.Locked(state.LockoutUntil.Value - now);
        }

        await cache.RemoveAsync(key, cancellationToken);
        return LoginThrottleDecision.Allowed;
    }

    public async Task RecordFailureAsync(string email, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(email);
        if (key is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var options = optionsAccessor.Value;
        var failureWindow = TimeSpan.FromMinutes(options.FailureWindowMinutes);
        var lockout = TimeSpan.FromMinutes(options.LockoutMinutes);

        var data = await cache.GetStringAsync(key, cancellationToken);
        LoginAttemptState state;

        if (data is not null)
        {
            state = JsonSerializer.Deserialize<LoginAttemptState>(data, JsonOptions)!;

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
        }
        else
        {
            state = new LoginAttemptState { FirstFailure = now };
        }

        state.FailedAttempts++;
        if (state.FailedAttempts >= options.MaxFailedAttempts)
        {
            state.LockoutUntil = now.Add(lockout);
        }

        var serialized = JsonSerializer.Serialize(state, JsonOptions);
        await cache.SetStringAsync(key, serialized, new DistributedCacheEntryOptions
        {
            SlidingExpiration = failureWindow + lockout
        }, cancellationToken);
    }

    public async Task RecordSuccessAsync(string email, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(email);
        if (key is not null)
        {
            await cache.RemoveAsync(key, cancellationToken);
        }
    }

    private static string? GetCacheKey(string email)
    {
        var normalized = IdentityUseCases.NormalizeEmail(email);
        return string.IsNullOrWhiteSpace(normalized) ? null : CacheKeys.Throttle(normalized);
    }

    private sealed class LoginAttemptState
    {
        public DateTimeOffset FirstFailure { get; set; }
        public int FailedAttempts { get; set; }
        public DateTimeOffset? LockoutUntil { get; set; }
    }
}
