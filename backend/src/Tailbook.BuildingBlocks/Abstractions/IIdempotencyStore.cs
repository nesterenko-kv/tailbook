using ErrorOr;

namespace Tailbook.BuildingBlocks.Abstractions;

public interface IIdempotencyStore
{
    Task<ErrorOr<IdempotencyAcquireResult>> TryAcquireAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task CompleteAsync(string idempotencyKey, int statusCode, string? responseBody, CancellationToken cancellationToken);
    Task CleanupExpiredAsync(CancellationToken cancellationToken);
}

public sealed record IdempotencyAcquireResult(
    bool IsNew,
    bool IsCompleted,
    int? ExistingStatusCode,
    string? ExistingResponseBody);
