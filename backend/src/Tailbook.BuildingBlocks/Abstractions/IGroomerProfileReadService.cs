namespace Tailbook.BuildingBlocks.Abstractions;

public interface IGroomerProfileReadService
{
    Task<GroomerProfileReadModel?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<GroomerProfileReadModel?> GetByGroomerIdAsync(Guid groomerId, CancellationToken cancellationToken);
}

public sealed record GroomerProfileReadModel(
    Guid GroomerId,
    Guid? UserId,
    string DisplayName,
    bool Active);
