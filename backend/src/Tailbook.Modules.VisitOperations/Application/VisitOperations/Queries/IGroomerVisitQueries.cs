using ErrorOr;

namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Queries;

public interface IGroomerVisitQueries
{
    Task<ErrorOr<GroomerVisitDetailView>> CheckInAppointmentAsync(Guid currentUserId, Guid appointmentId, CancellationToken cancellationToken);
    Task<ErrorOr<GroomerVisitDetailView>> GetVisitByAppointmentAsync(Guid currentUserId, Guid appointmentId, CancellationToken cancellationToken);
    Task<ErrorOr<GroomerVisitDetailView>> GetVisitAsync(Guid currentUserId, Guid visitId, CancellationToken cancellationToken);
    Task<ErrorOr<GroomerVisitDetailView>> RecordPerformedProcedureAsync(Guid currentUserId, Guid visitId, Guid visitExecutionItemId, Guid procedureId, string? note, CancellationToken cancellationToken);
    Task<ErrorOr<GroomerVisitDetailView>> RecordSkippedComponentAsync(Guid currentUserId, Guid visitId, Guid visitExecutionItemId, Guid offerVersionComponentId, string omissionReasonCode, string? note, CancellationToken cancellationToken);
}
