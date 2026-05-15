using System.Globalization;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Infrastructure;

namespace Tailbook.Api.Host.Infrastructure;

public sealed class DistributedRateLimitMiddleware(RequestDelegate next, IOptions<RateLimitOptions> optionsAccessor, ILogger<DistributedRateLimitMiddleware> logger)
{
    private readonly List<RateLimitRule> _rules = optionsAccessor.Value.Rules
        .OrderByDescending(r => r.PathPrefix.Trim('/').Length)
        .ToList();

    public async Task InvokeAsync(HttpContext context, IDistributedCache cache)
    {
        var path = context.Request.Path.Value?.Trim('/') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            await next(context);
            return;
        }

        var method = context.Request.Method;
        var rule = _rules.FirstOrDefault(r => path.StartsWith(r.PathPrefix.Trim('/'), StringComparison.OrdinalIgnoreCase));
        if (rule is null)
        {
            await next(context);
            return;
        }

        var clientIp = GetClientIp(context);
        var windowStart = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / rule.WindowSeconds * rule.WindowSeconds;
        var cacheKey = CacheKeys.RateLimit(clientIp, method, path, windowStart);

        var countStr = await cache.GetStringAsync(cacheKey, context.RequestAborted);
        var count = countStr is not null ? int.Parse(countStr, CultureInfo.InvariantCulture) : 0;

        if (count >= rule.PermitLimit)
        {
            logger.LogWarning("Rate limit exceeded for {Method} {Path} from {IP}", method, path, clientIp);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = rule.WindowSeconds.ToString(CultureInfo.InvariantCulture);
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                """{"type":"about:blank","title":"Too Many Requests","status":429,"detail":"Rate limit exceeded. Try again later."}""",
                Encoding.UTF8,
                context.RequestAborted);
            return;
        }

        await cache.SetStringAsync(cacheKey, (count + 1).ToString(CultureInfo.InvariantCulture), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(rule.WindowSeconds * 2)
        }, context.RequestAborted);

        await next(context);
    }

    private static string GetClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
