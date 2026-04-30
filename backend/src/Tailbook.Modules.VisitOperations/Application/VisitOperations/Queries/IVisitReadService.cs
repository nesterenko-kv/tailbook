using ErrorOr;

namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Queries;

public interface IVisitReadService
{
    Task<VisitDetailView?> GetVisitAsync(Guid visitId, Guid? actorUserId, CancellationToken cancellationToken, bool recordAccessAudit = true);
    Task<ErrorOr<PagedResult<VisitListItemView>>> ListVisitsAsync(string? status, DateTime? fromUtc, DateTime? toUtc, Guid? groomerId, Guid? appointmentId, int page, int pageSize, CancellationToken cancellationToken);
}
