namespace Tailbook.BuildingBlocks.Abstractions;

public interface IStaffSchedulingService
{
    Task<ReservedDurationResolution> ResolveReservedDurationAsync(
        Guid groomerId,
        Guid petId,
        IReadOnlyCollection<Guid> offerIds,
        int baseReservedMinutes,
        CancellationToken cancellationToken);

    Task<ReservedDurationResolution> ResolveReservedDurationAsync(
        Guid groomerId,
        PetQuoteProfile pet,
        IReadOnlyCollection<Guid> offerIds,
        int baseReservedMinutes,
        CancellationToken cancellationToken);

    Task<GroomerAvailabilityCheckResult> CheckAvailabilityAsync(
        Guid groomerId,
        Guid petId,
        IReadOnlyCollection<Guid> offerIds,
        DateTime startAtUtc,
        int reservedMinutes,
        Guid? ignoredAppointmentId,
        CancellationToken cancellationToken);

    Task<GroomerAvailabilityCheckResult> CheckAvailabilityAsync(
        Guid groomerId,
        PetQuoteProfile pet,
        IReadOnlyCollection<Guid> offerIds,
        DateTime startAtUtc,
        int reservedMinutes,
        Guid? ignoredAppointmentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AvailabilityWindowReadModel>> GetAvailabilityWindowsAsync(
        Guid groomerId,
        DateOnly localDate,
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

public sealed record AvailabilityWindowReadModel(
    DateTime StartAtUtc,
    DateTime EndAtUtc);
