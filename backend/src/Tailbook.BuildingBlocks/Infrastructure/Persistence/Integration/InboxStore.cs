using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

public sealed class InboxStore(
    AppDbContext dbContext,
    IDistributedCache cache) : IInboxStore
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    public async Task<bool> TryReceiveAsync(string messageId, string consumerName, string eventType, string payloadJson, CancellationToken ct)
    {
        var cacheKey = CacheKeys.InboxMessage(messageId, consumerName);

        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null) return false;

        dbContext.Set<InboxMessage>().Add(new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            ConsumerName = consumerName,
            EventType = eventType,
            PayloadJson = payloadJson,
            Status = "Received",
            ReceivedAt = DateTimeOffset.UtcNow
        });

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return false;
        }

        await cache.SetStringAsync(cacheKey, "Received", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl
        }, ct);

        return true;
    }
}
