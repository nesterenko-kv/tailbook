using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Services;

public sealed class GroomerVisitReadService(
    AppDbContext dbContext,
    IVisitReadService visitReadService,
    IAppointmentVisitService appointmentVisitService,
    IGroomerProfileReadService groomerProfileReadService) : IGroomerVisitReadService
{
    public async Task<ErrorOr<GroomerVisitDetailView>> GetVisitByAppointmentAsync(Guid currentUserId, Guid appointmentId, CancellationToken cancellationToken)
    {
        var groomer = await groomerProfileReadService.GetByUserIdAsync(currentUserId, cancellationToken);
        if (groomer.IsError)
        {
            return groomer.Errors;
        }

        var appointment = await appointmentVisitService.GetAppointmentAsync(appointmentId, cancellationToken);
        if (appointment is null || appointment.GroomerId != groomer.Value.GroomerId)
        {
            return Error.NotFound("VisitOperations.AppointmentNotFound", "Appointment does not exist.");
        }

        var visitId = await dbContext.Set<Visit>()
            .Where(x => x.AppointmentId == appointmentId)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (!visitId.HasValue)
        {
            return Error.NotFound("VisitOperations.VisitNotFound", "Visit does not exist.");
        }

        var result = await visitReadService.GetVisitAsync(visitId.Value, currentUserId, cancellationToken, recordAccessAudit: false);
        return result is null
            ? Error.NotFound("VisitOperations.VisitNotFound", "Visit does not exist.")
            : GroomerVisitMapper.Map(result);
    }

    public async Task<ErrorOr<GroomerVisitDetailView>> GetVisitAsync(Guid currentUserId, Guid visitId, CancellationToken cancellationToken)
    {
        var groomer = await groomerProfileReadService.GetByUserIdAsync(currentUserId, cancellationToken);
        if (groomer.IsError)
        {
            return groomer.Errors;
        }

        var result = await visitReadService.GetVisitAsync(visitId, currentUserId, cancellationToken, recordAccessAudit: false);
        if (result is null || result.GroomerId != groomer.Value.GroomerId)
        {
            return Error.NotFound("VisitOperations.VisitNotFound", "Visit does not exist.");
        }

        return GroomerVisitMapper.Map(result);
    }
}
