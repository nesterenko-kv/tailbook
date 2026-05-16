using System.Text.Json;
using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Booking.Infrastructure.Services.CommandHandlers;

public sealed class RescheduleAppointmentUseCaseCommandHandler(
    AppDbContext dbContext,
    IBookingManagementReadService bookingReadService,
    IStaffSchedulingService staffSchedulingService,
    IAuditTrailService auditTrailService,
    IOutboxPublisher outboxPublisher,
    TimeProvider timeProvider)
    : ICommandHandler<RescheduleAppointmentUseCaseCommand, ErrorOr<AppointmentDetailView>>
{
    public async Task<ErrorOr<AppointmentDetailView>> ExecuteAsync(RescheduleAppointmentUseCaseCommand command, CancellationToken ct = default)
    {
        var appointment = await dbContext.Set<Appointment>().SingleOrDefaultAsync(x => x.Id == command.AppointmentId, ct);
        if (appointment is null)
        {
            return Error.NotFound("Booking.AppointmentNotFound", "Appointment does not exist.");
        }

        var version = EnsureVersion(appointment, command.ExpectedVersionNo);
        if (version.IsError)
        {
            return version.Errors;
        }

        if (appointment.Status is AppointmentStatusCodes.Cancelled or AppointmentStatusCodes.Closed)
        {
            return Error.Conflict("Booking.AppointmentNotMutable", "Appointment is not mutable in its current status.");
        }

        var normalizedStartAtResult = BookingTimeInputNormalizer.AssumeUtc(command.StartAt, nameof(command.StartAt));
        if (normalizedStartAtResult.IsError)
        {
            return normalizedStartAtResult.Errors;
        }

        var normalizedStartAt = normalizedStartAtResult.Value;

        var items = await dbContext.Set<AppointmentItem>()
            .Where(x => x.AppointmentId == appointment.Id)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        var durationSnapshots = await dbContext.Set<DurationSnapshot>()
            .Where(x => items.Select(y => y.DurationSnapshotId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        var modifierLines = await dbContext.Set<DurationSnapshotLine>()
            .Where(x => items.Select(y => y.DurationSnapshotId).Contains(x.DurationSnapshotId))
            .Where(x => x.LineType == "GroomerCapabilityModifier")
            .ToListAsync(ct);

        var baseReservedMinutes = items.Sum(x =>
        {
            var snapshot = durationSnapshots.GetValueOrDefault(x.DurationSnapshotId);
            var modifierTotal = modifierLines
                .Where(y => y.DurationSnapshotId == x.DurationSnapshotId)
                .Sum(y => y.Minutes);
            return (snapshot?.ReservedMinutes ?? 0) - modifierTotal;
        });

        var availabilityResult = await staffSchedulingService.CheckAvailabilityAsync(
            command.GroomerId,
            appointment.PetId,
            items.Select(x => x.OfferId).ToArray(),
            normalizedStartAt,
            baseReservedMinutes,
            appointment.Id,
            ct);
        if (availabilityResult.IsError)
        {
            return availabilityResult.Errors;
        }

        var availability = availabilityResult.Value;
        if (!availability.IsAvailable)
        {
            return Error.Validation("Booking.AppointmentSlotUnavailable", string.Join(" ", availability.Reasons));
        }

        var appointmentPeriod = BookingTimeInputNormalizer.CreatePeriod(normalizedStartAt, availability.EndAt);
        if (appointmentPeriod.IsError)
        {
            return appointmentPeriod.Errors;
        }

        var rescheduleResult = appointment.Reschedule(
            command.GroomerId,
            appointmentPeriod.Value,
            command.ActorUserId,
            timeProvider.GetUtcNow());
        if (rescheduleResult.IsError)
        {
            return rescheduleResult.Errors;
        }

        await outboxPublisher.PublishAsync("booking", "AppointmentRescheduled", new
        {
            appointmentId = appointment.Id,
            groomerId = appointment.GroomerId,
            startAt = appointment.StartAt,
            endAt = appointment.EndAt,
            versionNo = appointment.VersionNo
        }, ct);
        var saveResult = await ConcurrencySafeSaver.SaveAsync(dbContext, ct);
        if (saveResult.IsError)
        {
            return saveResult.Errors;
        }
        await auditTrailService.RecordAsync(
            "booking",
            "appointment",
            appointment.Id.ToString("D"),
            "RESCHEDULE",
            command.ActorUserId,
            null,
            JsonSerializer.Serialize(new { appointment.VersionNo, appointment.StartAt, appointment.EndAt }),
            ct);

        return (await bookingReadService.GetAppointmentAsync(appointment.Id, ct))!;
    }

    private static ErrorOr<Success> EnsureVersion(Appointment appointment, int expectedVersionNo)
    {
        if (!appointment.HasVersion(expectedVersionNo))
        {
            return AppointmentErrors.VersionMismatch(expectedVersionNo, appointment.VersionNo);
        }

        return Result.Success;
    }
}
