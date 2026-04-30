using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Contracts;

namespace Tailbook.Modules.Booking.Infrastructure.Services;

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

    public async Task<ErrorOr<bool>> MarkCheckedInAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        if (appointment.IsError)
        {
            return appointment.Errors;
        }

        if (appointment.Value.Status is not AppointmentStatusCodes.Confirmed and not AppointmentStatusCodes.Rescheduled)
        {
            return Error.Conflict("Booking.AppointmentCheckInNotAllowed", "Appointment is not eligible for check-in.");
        }

        appointment.Value.MarkCheckedIn(actorUserId, DateTime.UtcNow);
        return true;
    }

    public async Task<ErrorOr<bool>> MarkInProgressAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        if (appointment.IsError)
        {
            return appointment.Errors;
        }

        if (appointment.Value.Status == AppointmentStatusCodes.InProgress)
        {
            return true;
        }

        if (appointment.Value.Status != AppointmentStatusCodes.CheckedIn)
        {
            return Error.Conflict("Booking.AppointmentInProgressNotAllowed", "Appointment is not eligible to enter in-progress state.");
        }

        appointment.Value.MarkInProgress(actorUserId, DateTime.UtcNow);
        return true;
    }

    public async Task<ErrorOr<bool>> MarkCompletedAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        if (appointment.IsError)
        {
            return appointment.Errors;
        }

        if (appointment.Value.Status is not AppointmentStatusCodes.CheckedIn and not AppointmentStatusCodes.InProgress)
        {
            return Error.Conflict("Booking.AppointmentCompletionNotAllowed", "Appointment is not eligible for completion.");
        }

        appointment.Value.MarkCompleted(actorUserId, DateTime.UtcNow);
        return true;
    }

    public async Task<ErrorOr<bool>> MarkClosedAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        if (appointment.IsError)
        {
            return appointment.Errors;
        }

        if (appointment.Value.Status != AppointmentStatusCodes.Completed)
        {
            return Error.Conflict("Booking.AppointmentClosureNotAllowed", "Appointment is not eligible for closure.");
        }

        appointment.Value.MarkClosed(actorUserId, DateTime.UtcNow);
        return true;
    }

    private async Task<ErrorOr<Appointment>> LoadAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.Set<Appointment>().SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);
        return appointment is null
            ? Error.NotFound("Booking.AppointmentNotFound", "Appointment does not exist.")
            : appointment;
    }
}
