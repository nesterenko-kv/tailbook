using System.Collections.Immutable;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Application.Common.Errors;

namespace Tailbook.Modules.Booking.Infrastructure.Services;

public sealed class AppointmentVisitService(AppDbContext dbContext, TimeProvider timeProvider) : IAppointmentVisitService
{
    public async Task<VisitAppointmentInfo?> GetAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointments = await ListAppointmentsAsync([appointmentId], null, null, null, cancellationToken);
        return appointments.GetValueOrDefault(appointmentId);
    }

    public async Task<IReadOnlyDictionary<Guid, VisitAppointmentInfo>> ListAppointmentsAsync(
        IReadOnlyCollection<Guid> appointmentIds,
        DateTimeOffset? from,
        DateTimeOffset? to,
        Guid? groomerId,
        CancellationToken cancellationToken)
    {
        if (appointmentIds.Count == 0)
        {
            return ImmutableDictionary<Guid, VisitAppointmentInfo>.Empty;
        }

        var query = dbContext.Set<Appointment>()
            .Where(x => appointmentIds.Contains(x.Id));

        if (from.HasValue)
        {
            query = query.Where(x => x.StartAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.StartAt < to.Value);
        }

        if (groomerId.HasValue)
        {
            query = query.Where(x => x.GroomerId == groomerId.Value);
        }

        var appointments = await query.ToListAsync(cancellationToken);
        var matchedAppointmentIds = appointments.Select(x => x.Id).ToArray();

        var items = await dbContext.Set<AppointmentItem>()
            .Where(x => matchedAppointmentIds.Contains(x.AppointmentId))
            .OrderBy(x => x.CreatedAt)
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
                x.StartAt,
                x.EndAt,
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
                        priceSnapshots.GetValueOrDefault(item.PriceSnapshotId)?.TotalAmount ?? 0m,
                        durationSnapshots.GetValueOrDefault(item.DurationSnapshotId)?.ServiceMinutes ?? 0,
                        durationSnapshots.GetValueOrDefault(item.DurationSnapshotId)?.ReservedMinutes ?? 0))
                    .ToArray()));
    }

    public async Task<ErrorOr<Success>> MarkCheckedInAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        if (appointment.IsError)
        {
            return appointment.Errors;
        }

        var result = appointment.Value.MarkCheckedIn(actorUserId, timeProvider.GetUtcNow());
        if (result.IsError)
        {
            return result.Errors;
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> MarkInProgressAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        if (appointment.IsError)
        {
            return appointment.Errors;
        }

        var result = appointment.Value.MarkInProgress(actorUserId, timeProvider.GetUtcNow());
        if (result.IsError)
        {
            return result.Errors;
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> MarkCompletedAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        if (appointment.IsError)
        {
            return appointment.Errors;
        }

        var result = appointment.Value.MarkCompleted(actorUserId, timeProvider.GetUtcNow());
        if (result.IsError)
        {
            return result.Errors;
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> MarkClosedAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        if (appointment.IsError)
        {
            return appointment.Errors;
        }

        var result = appointment.Value.MarkClosed(actorUserId, timeProvider.GetUtcNow());
        if (result.IsError)
        {
            return result.Errors;
        }

        return Result.Success;
    }

    private async Task<ErrorOr<Appointment>> LoadAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.Set<Appointment>().SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);
        return appointment is null
            ? AppointmentErrors.NotFound
            : appointment;
    }
}
