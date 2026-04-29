using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Domain;

namespace Tailbook.Modules.Booking.Application;

public sealed class AppointmentVisitService(AppDbContext dbContext) : IAppointmentVisitService
{
    public async Task<VisitAppointmentInfo?> GetAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointments = await ListAppointmentsAsync([appointmentId], null, null, null, cancellationToken);
        return appointments.GetValueOrDefault(appointmentId);
    }

    public async Task<IReadOnlyDictionary<Guid, VisitAppointmentInfo>> ListAppointmentsAsync(
        IReadOnlyCollection<Guid> appointmentIds,
        DateTime? fromUtc,
        DateTime? toUtc,
        Guid? groomerId,
        CancellationToken cancellationToken)
    {
        if (appointmentIds.Count == 0)
        {
            return new Dictionary<Guid, VisitAppointmentInfo>();
        }

        var query = dbContext.Set<Appointment>()
            .Where(x => appointmentIds.Contains(x.Id));

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.StartAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.StartAtUtc < toUtc.Value);
        }

        if (groomerId.HasValue)
        {
            query = query.Where(x => x.GroomerId == groomerId.Value);
        }

        var appointments = await query.ToListAsync(cancellationToken);
        var matchedAppointmentIds = appointments.Select(x => x.Id).ToArray();

        var items = await dbContext.Set<AppointmentItem>()
            .Where(x => matchedAppointmentIds.Contains(x.AppointmentId))
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var priceSnapshots = await dbContext.Set<PriceSnapshot>()
            .Where(x => items.Select(y => y.PriceSnapshotId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var durationSnapshots = await dbContext.Set<DurationSnapshot>()
            .Where(x => items.Select(y => y.DurationSnapshotId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return appointments.ToDictionary(
            x => x.Id,
            x => new VisitAppointmentInfo(
                x.Id,
                x.BookingRequestId,
                x.PetId,
                x.GroomerId,
                x.StartAtUtc,
                x.EndAtUtc,
                x.Status,
                x.VersionNo,
                items.Where(item => item.AppointmentId == x.Id)
                    .Select(item => new VisitAppointmentItemInfo(
                        item.Id,
                        item.ItemType,
                        item.OfferId,
                        item.OfferVersionId,
                        item.OfferCodeSnapshot,
                        item.OfferDisplayNameSnapshot,
                        item.Quantity,
                        item.PriceSnapshotId,
                        item.DurationSnapshotId,
                        priceSnapshots[item.PriceSnapshotId].TotalAmount,
                        durationSnapshots[item.DurationSnapshotId].ServiceMinutes,
                        durationSnapshots[item.DurationSnapshotId].ReservedMinutes))
                    .ToArray()));
    }

    public async Task MarkCheckedInAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        appointment.MarkCheckedIn(actorUserId, DateTime.UtcNow);
    }

    public async Task MarkInProgressAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        appointment.MarkInProgress(actorUserId, DateTime.UtcNow);
    }

    public async Task MarkCompletedAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        appointment.MarkCompleted(actorUserId, DateTime.UtcNow);
    }

    public async Task MarkClosedAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        appointment.MarkClosed(actorUserId, DateTime.UtcNow);
    }

    private async Task<Appointment> LoadAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<Appointment>().SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken)
               ?? throw new InvalidOperationException("Appointment does not exist.");
    }
}
