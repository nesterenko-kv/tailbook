using ErrorOr;

namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Queries;

public interface IVisitQueries
{
    Task<ErrorOr<VisitDetailView>> CheckInAppointmentAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken);
    Task<VisitDetailView?> GetVisitAsync(Guid visitId, Guid? actorUserId, CancellationToken cancellationToken, bool recordAccessAudit = true);
    Task<ErrorOr<PagedResult<VisitListItemView>>> ListVisitsAsync(string? status, DateTime? fromUtc, DateTime? toUtc, Guid? groomerId, Guid? appointmentId, int page, int pageSize, CancellationToken cancellationToken);
    Task<ErrorOr<VisitDetailView>> RecordPerformedProcedureAsync(Guid visitId, Guid visitExecutionItemId, Guid procedureId, string? note, Guid? actorUserId, CancellationToken cancellationToken);
    Task<ErrorOr<VisitDetailView>> RecordSkippedComponentAsync(Guid visitId, Guid visitExecutionItemId, Guid offerVersionComponentId, string omissionReasonCode, string? note, Guid? actorUserId, CancellationToken cancellationToken);
    Task<ErrorOr<VisitDetailView>> ApplyPriceAdjustmentAsync(Guid visitId, int sign, decimal amount, string reasonCode, string? note, Guid? actorUserId, CancellationToken cancellationToken);
    Task<ErrorOr<VisitDetailView>> CompleteVisitAsync(Guid visitId, Guid? actorUserId, CancellationToken cancellationToken);
    Task<ErrorOr<VisitDetailView>> CloseVisitAsync(Guid visitId, Guid? actorUserId, CancellationToken cancellationToken);
}
