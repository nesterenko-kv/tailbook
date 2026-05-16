using System.Text.Json;
using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Booking.Infrastructure.Services.CommandHandlers;

public sealed class CancelAppointmentUseCaseCommandHandler(
    AppDbContext dbContext,
    IBookingManagementReadService bookingReadService,
    IAuditTrailService auditTrailService,
    IOutboxPublisher outboxPublisher,
    TimeProvider timeProvider)
    : ICommandHandler<CancelAppointmentUseCaseCommand, ErrorOr<AppointmentDetailView>>
{
    public async Task<ErrorOr<AppointmentDetailView>> ExecuteAsync(CancelAppointmentUseCaseCommand command, CancellationToken ct = default)
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
            return Error.Conflict("Booking.AppointmentCancellationFailed", "Appointment is not mutable in its current status.");
        }

        if (string.IsNullOrWhiteSpace(command.ReasonCode))
        {
            return Error.Validation("Booking.CancellationReasonRequired", "Cancellation reason code is required.");
        }

        var cancelResult = appointment.Cancel(command.ReasonCode, command.Notes, command.ActorUserId, timeProvider.GetUtcNow());
        if (cancelResult.IsError)
        {
            return cancelResult.Errors;
        }

        await outboxPublisher.PublishAsync("booking", "AppointmentCancelled", new
        {
            appointmentId = appointment.Id,
            status = appointment.Status,
            reasonCode = appointment.CancellationReasonCode,
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
            "CANCEL",
            command.ActorUserId,
            null,
            JsonSerializer.Serialize(new { appointment.CancellationReasonCode, appointment.VersionNo }),
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
