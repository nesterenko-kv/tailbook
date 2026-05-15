using ErrorOr;

namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Queries;

public interface IGroomerVisitReadService
{
    Task<ErrorOr<GroomerVisitDetailView>> GetVisitByAppointmentAsync(Guid currentUserId, Guid appointmentId, CancellationToken cancellationToken);
    Task<ErrorOr<GroomerVisitDetailView>> GetVisitAsync(Guid currentUserId, Guid visitId, CancellationToken cancellationToken);
}
