using System.Text.Json;
using ErrorOr;
using FastEndpoints;
using Tailbook.Modules.Booking.Api.Contracts.Admin.BulkCancelAppointments;

namespace Tailbook.Modules.Booking.Infrastructure.Services.CommandHandlers;

public sealed class BulkCancelAppointmentsUseCaseCommandHandler(
    AppDbContext dbContext,
    IAuditTrailService auditTrailService,
    TimeProvider timeProvider
)
    : ICommandHandler<BulkCancelAppointmentsUseCaseCommand, ErrorOr<BulkCancelAppointmentsResponse>>
{
    public async Task<ErrorOr<BulkCancelAppointmentsResponse>> ExecuteAsync(BulkCancelAppointmentsUseCaseCommand command, CancellationToken ct = default)
    {
        var response = new BulkCancelAppointmentsResponse();

        var appointments = await dbContext.Set<Appointment>()
            .Where(x => command.AppointmentIds.Contains(x.Id))
            .ToListAsync(ct);

        var notFoundIds = command.AppointmentIds
            .Where(id => appointments.All(a => a.Id != id))
            .ToList();

        foreach (var notFoundId in notFoundIds)
        {
            response.Failed++;
            response.Errors.Add(new BulkCancelAppointmentErrorItem
            {
                AppointmentId = notFoundId.ToString("D"),
                ErrorMessage = "Appointment does not exist."
            });
        }

        foreach (var appointment in appointments)
        {
            if (appointment.Status is AppointmentStatusCodes.Cancelled or AppointmentStatusCodes.Closed)
            {
                response.Failed++;
                response.Errors.Add(new BulkCancelAppointmentErrorItem
                {
                    AppointmentId = appointment.Id.ToString("D"),
                    ErrorMessage = "Appointment is not mutable in its current status."
                });
                continue;
            }

            var cancelResult = appointment.Cancel(command.ReasonCode, command.Notes, command.ActorUserId, timeProvider.GetUtcNow());
            if (cancelResult.IsError)
            {
                response.Failed++;
                foreach (var error in cancelResult.Errors)
                {
                    response.Errors.Add(new BulkCancelAppointmentErrorItem
                    {
                        AppointmentId = appointment.Id.ToString("D"),
                        ErrorMessage = error.Description
                    });
                }
                continue;
            }

            response.Succeeded++;
        }

        var saveResult = await ConcurrencySafeSaver.SaveAsync(dbContext, ct);
        if (saveResult.IsError)
        {
            return saveResult.Errors;
        }

        foreach (var appointment in appointments.Where(a => a.Status is AppointmentStatusCodes.Cancelled))
        {
            await auditTrailService.RecordAsync(
                "booking",
                "appointment",
                appointment.Id.ToString("D"),
                "CANCEL",
                command.ActorUserId,
                null,
                JsonSerializer.Serialize(new { appointment.CancellationReasonCode, appointment.VersionNo }),
                ct
            );
        }

        return response;
    }
}
