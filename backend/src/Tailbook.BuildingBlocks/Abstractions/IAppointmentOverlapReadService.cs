namespace Tailbook.BuildingBlocks.Abstractions;

public interface IAppointmentOverlapReadService
{
    Task<bool> HasOverlapAsync(
        Guid groomerId,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        Guid? ignoredAppointmentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AppointmentBusyIntervalReadModel>> ListBusyIntervalsAsync(
        Guid groomerId,
        DateTimeOffset from,
        DateTimeOffset to,
        Guid? ignoredAppointmentId,
        CancellationToken cancellationToken);
}

public sealed record AppointmentBusyIntervalReadModel(
    Guid AppointmentId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt);
