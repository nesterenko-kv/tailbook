using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

public sealed class IdempotencyStore(
    AppDbContext dbContext,
    TimeProvider timeProvider,
    Microsoft.Extensions.Options.IOptions<IdempotencyRequestOptions> optionsAccessor) : IIdempotencyStore
{
    private readonly IdempotencyRequestOptions _options = optionsAccessor.Value;

    public async Task<ErrorOr<IdempotencyAcquireResult>> TryAcquireAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Set<IdempotentRequest>()
            .Where(x => x.IdempotencyKey == idempotencyKey)
            .Select(x => new { x.Status, x.ResponseStatusCode, x.ResponseBody })
            .SingleOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            dbContext.Set<IdempotentRequest>().Add(new IdempotentRequest
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = idempotencyKey,
                Status = IdempotentRequestStatuses.Processing,
                CreatedAt = timeProvider.GetUtcNow(),
                ExpiresAt = timeProvider.GetUtcNow().Add(_options.EntryTtl)
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            return new IdempotencyAcquireResult(IsNew: true, IsCompleted: false, null, null);
        }

        if (existing.Status == IdempotentRequestStatuses.Completed)
        {
            return new IdempotencyAcquireResult(IsNew: false, IsCompleted: true, existing.ResponseStatusCode, existing.ResponseBody);
        }

        return new IdempotencyAcquireResult(IsNew: false, IsCompleted: false, null, null);
    }

    public async Task CompleteAsync(string idempotencyKey, int statusCode, string? responseBody, CancellationToken cancellationToken)
    {
        var record = await dbContext.Set<IdempotentRequest>()
            .Where(x => x.IdempotencyKey == idempotencyKey)
            .FirstOrDefaultAsync(cancellationToken);

        if (record is null)
        {
            return;
        }

        record.Status = IdempotentRequestStatuses.Completed;
        record.ResponseStatusCode = statusCode;
        record.ResponseBody = responseBody;
        record.CompletedAt = timeProvider.GetUtcNow();

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CleanupExpiredAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var expired = await dbContext.Set<IdempotentRequest>()
            .Where(x => x.ExpiresAt != null && x.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (expired.Count > 0)
        {
            dbContext.Set<IdempotentRequest>().RemoveRange(expired);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

public sealed class IdempotencyRequestOptions
{
    public const string SectionName = "Idempotency";
    public TimeSpan EntryTtl { get; set; } = TimeSpan.FromDays(1);
}
