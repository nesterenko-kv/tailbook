using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tailbook.Api.Host.Infrastructure;

public sealed class RedisHealthCheck(IDistributedCache cache) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"health:{Guid.NewGuid():N}";
            await cache.SetStringAsync(key, "ok", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            }, cancellationToken);

            var value = await cache.GetStringAsync(key, cancellationToken);
            return value == "ok"
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Cache returned unexpected value.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cache ping failed.", ex);
        }
    }
}
