using ErrorOr;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Staff.Application.Staff.Queries;

public interface IStaffReadService
{
    Task<IReadOnlyCollection<GroomerListItemView>> ListGroomersAsync(CancellationToken cancellationToken);
    Task<GroomerDetailView?> GetGroomerAsync(Guid groomerId, CancellationToken cancellationToken);
    Task<ErrorOr<GroomerScheduleView>> GetScheduleAsync(Guid groomerId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);
    Task<ErrorOr<GroomerAvailabilityCheckResult>> CheckAvailabilityAsync(CheckGroomerAvailabilityQuery query, CancellationToken cancellationToken);
}
