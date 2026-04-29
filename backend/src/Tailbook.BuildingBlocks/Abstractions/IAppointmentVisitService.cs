namespace Tailbook.BuildingBlocks.Abstractions;

public interface IAppointmentVisitService
{
    Task<VisitAppointmentInfo?> GetAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, VisitAppointmentInfo>> ListAppointmentsAsync(
        IReadOnlyCollection<Guid> appointmentIds,
        DateTime? fromUtc,
        DateTime? toUtc,
        Guid? groomerId,
        CancellationToken cancellationToken);
    Task MarkCheckedInAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken);
    Task MarkInProgressAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken);
    Task MarkCompletedAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken);
    Task MarkClosedAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken);
}

public sealed record VisitAppointmentInfo(
    Guid AppointmentId,
    Guid? BookingRequestId,
    Guid PetId,
    Guid GroomerId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string Status,
    int VersionNo,
    IReadOnlyCollection<VisitAppointmentItemInfo> Items);

public sealed record VisitAppointmentItemInfo(
    Guid AppointmentItemId,
    string ItemType,
    Guid OfferId,
    Guid OfferVersionId,
    string OfferCode,
    string OfferDisplayName,
    int Quantity,
    Guid PriceSnapshotId,
    Guid DurationSnapshotId,
    decimal PriceAmount,
    int ServiceMinutes,
    int ReservedMinutes);
