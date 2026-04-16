namespace Tailbook.BuildingBlocks.Abstractions;

public interface IAppointmentOverlapReadService
{
    Task<bool> HasOverlapAsync(
        Guid groomerId,
        DateTime startAtUtc,
        DateTime endAtUtc,
        Guid? ignoredAppointmentId,
        CancellationToken cancellationToken);
}
