namespace Tailbook.BuildingBlocks.Abstractions;

public interface IStaffSchedulingService
{
    Task<ReservedDurationResolution> ResolveReservedDurationAsync(
        Guid groomerId,
        Guid petId,
        IReadOnlyCollection<Guid> offerIds,
        int baseReservedMinutes,
        CancellationToken cancellationToken);

    Task<GroomerAvailabilityCheckResult> CheckAvailabilityAsync(
        Guid groomerId,
        Guid petId,
        IReadOnlyCollection<Guid> offerIds,
        DateTime startAtUtc,
        int reservedMinutes,
        CancellationToken cancellationToken);
}

public sealed record ReservedDurationResolution(
    int BaseReservedMinutes,
    int EffectiveReservedMinutes,
    int ModifierMinutes,
    IReadOnlyCollection<string> Reasons);

public sealed record GroomerAvailabilityCheckResult(
    bool IsAvailable,
    DateTime EndAtUtc,
    int CheckedReservedMinutes,
    IReadOnlyCollection<string> Reasons);
