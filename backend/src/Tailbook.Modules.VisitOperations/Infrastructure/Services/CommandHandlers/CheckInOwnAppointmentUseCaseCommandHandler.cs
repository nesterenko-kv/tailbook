using ErrorOr;
using FastEndpoints;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Services.CommandHandlers;

public sealed class CheckInOwnAppointmentUseCaseCommandHandler(
    CheckInAppointmentUseCaseCommandHandler checkInAppointmentHandler,
    IAppointmentVisitService appointmentVisitService,
    IGroomerProfileReadService groomerProfileReadService)
    : ICommandHandler<CheckInOwnAppointmentUseCaseCommand, ErrorOr<GroomerVisitDetailView>>
{
    public async Task<ErrorOr<GroomerVisitDetailView>> ExecuteAsync(CheckInOwnAppointmentUseCaseCommand command, CancellationToken ct = default)
    {
        var groomer = await groomerProfileReadService.GetByUserIdAsync(command.CurrentUserId, ct);
        if (groomer.IsError)
        {
            return groomer.Errors;
        }

        var appointment = await appointmentVisitService.GetAppointmentAsync(command.AppointmentId, ct);
        if (appointment is null || appointment.GroomerId != groomer.Value.GroomerId)
        {
            return Error.NotFound("VisitOperations.AppointmentNotFound", "Appointment does not exist.");
        }

        var result = await checkInAppointmentHandler.ExecuteAsync(
            new CheckInAppointmentUseCaseCommand(command.AppointmentId, command.CurrentUserId),
            ct);

        return result.IsError
            ? result.Errors
            : GroomerVisitMapper.Map(result.Value);
    }
}
