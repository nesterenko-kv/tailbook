using System.Text.Json;
using ErrorOr;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

public sealed class IdempotencyStore(
    IDistributedCache cache,
    IOptions<IdempotencyRequestOptions> optionsAccessor) : IIdempotencyStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IdempotencyRequestOptions _options = optionsAccessor.Value;

    public async Task<ErrorOr<IdempotencyAcquireResult>> TryAcquireAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Idempotency(idempotencyKey);

        var existing = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (existing is null)
        {
            await cache.SetStringAsync(cacheKey, IdempotentRequestStatuses.Processing, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.EntryTtl
            }, cancellationToken);

            return new IdempotencyAcquireResult(IsNew: true, IsCompleted: false, null, null);
        }

        if (existing == IdempotentRequestStatuses.Processing)
        {
            return new IdempotencyAcquireResult(IsNew: false, IsCompleted: false, null, null);
        }

        var completed = JsonSerializer.Deserialize<CachedResponse>(existing, JsonOptions);
        return new IdempotencyAcquireResult(IsNew: false, IsCompleted: true, completed?.StatusCode, completed?.ResponseBody);
    }

    public async Task CompleteAsync(string idempotencyKey, int statusCode, string? responseBody, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Idempotency(idempotencyKey);
        var cached = new CachedResponse
        {
            StatusCode = statusCode,
            ResponseBody = responseBody
        };
        var serialized = JsonSerializer.Serialize(cached, JsonOptions);

        await cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _options.EntryTtl
        }, cancellationToken);
    }

    private sealed class CachedResponse
    {
        public int StatusCode { get; set; }
        public string? ResponseBody { get; set; }
    }
}

public sealed class IdempotencyRequestOptions
{
    public const string SectionName = "Idempotency";
    public TimeSpan EntryTtl { get; set; } = TimeSpan.FromDays(1);
}
