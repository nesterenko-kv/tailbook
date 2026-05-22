using ErrorOr;

namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IDeviceTrustService
{
    Task<ErrorOr<string>> IssueTrustTokenAsync(Guid userId, string surface, string? label, CancellationToken cancellationToken);
    ValueTask<bool> IsDeviceTrustedAsync(string deviceToken, string surface, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DeviceTrustView>> ListTrustsAsync(Guid userId, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> RevokeTrustAsync(Guid trustId, Guid userId, CancellationToken cancellationToken);
}

public sealed record DeviceTrustView(Guid Id, string Surface, string? Label, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, DateTimeOffset? LastUsedAt);
