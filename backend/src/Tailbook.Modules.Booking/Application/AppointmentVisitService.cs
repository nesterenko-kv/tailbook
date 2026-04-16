using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Contracts;
using Tailbook.Modules.Booking.Domain;

namespace Tailbook.Modules.Booking.Application;

public sealed class AppointmentVisitService(AppDbContext dbContext) : IAppointmentVisitService
{
    public async Task<VisitAppointmentInfo?> GetAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.Set<Appointment>()
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return null;
        }

        var items = await dbContext.Set<AppointmentItem>()
            .Where(x => x.AppointmentId == appointmentId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var priceSnapshots = await dbContext.Set<PriceSnapshot>()
            .Where(x => items.Select(y => y.PriceSnapshotId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var durationSnapshots = await dbContext.Set<DurationSnapshot>()
            .Where(x => items.Select(y => y.DurationSnapshotId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return new VisitAppointmentInfo(
            appointment.Id,
            appointment.BookingRequestId,
            appointment.PetId,
            appointment.GroomerId,
            appointment.StartAtUtc,
            appointment.EndAtUtc,
            appointment.Status,
            appointment.VersionNo,
            items.Select(x => new VisitAppointmentItemInfo(
                x.Id,
                x.ItemType,
                x.OfferId,
                x.OfferVersionId,
                x.OfferCodeSnapshot,
                x.OfferDisplayNameSnapshot,
                x.Quantity,
                x.PriceSnapshotId,
                x.DurationSnapshotId,
                priceSnapshots[x.PriceSnapshotId].TotalAmount,
                durationSnapshots[x.DurationSnapshotId].ServiceMinutes,
                durationSnapshots[x.DurationSnapshotId].ReservedMinutes))
                .ToArray());
    }

    public async Task MarkCheckedInAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        if (appointment.Status is not AppointmentStatusCodes.Confirmed and not AppointmentStatusCodes.Rescheduled)
        {
            throw new InvalidOperationException("Appointment is not eligible for check-in.");
        }

        appointment.Status = AppointmentStatusCodes.CheckedIn;
        appointment.VersionNo += 1;
        appointment.UpdatedAtUtc = DateTime.UtcNow;
        appointment.UpdatedByUserId = actorUserId;
    }

    public async Task MarkInProgressAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        if (appointment.Status == AppointmentStatusCodes.InProgress)
        {
            return;
        }

        if (appointment.Status != AppointmentStatusCodes.CheckedIn)
        {
            throw new InvalidOperationException("Appointment is not eligible to enter in-progress state.");
        }

        appointment.Status = AppointmentStatusCodes.InProgress;
        appointment.VersionNo += 1;
        appointment.UpdatedAtUtc = DateTime.UtcNow;
        appointment.UpdatedByUserId = actorUserId;
    }

    public async Task MarkCompletedAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        if (appointment.Status is not AppointmentStatusCodes.CheckedIn and not AppointmentStatusCodes.InProgress)
        {
            throw new InvalidOperationException("Appointment is not eligible for completion.");
        }

        appointment.Status = AppointmentStatusCodes.Completed;
        appointment.VersionNo += 1;
        appointment.UpdatedAtUtc = DateTime.UtcNow;
        appointment.UpdatedByUserId = actorUserId;
    }

    public async Task MarkClosedAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(appointmentId, cancellationToken);
        if (appointment.Status != AppointmentStatusCodes.Completed)
        {
            throw new InvalidOperationException("Appointment is not eligible for closure.");
        }

        appointment.Status = AppointmentStatusCodes.Closed;
        appointment.VersionNo += 1;
        appointment.UpdatedAtUtc = DateTime.UtcNow;
        appointment.UpdatedByUserId = actorUserId;
    }

    private async Task<Appointment> LoadAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<Appointment>().SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken)
               ?? throw new InvalidOperationException("Appointment does not exist.");
    }
}
