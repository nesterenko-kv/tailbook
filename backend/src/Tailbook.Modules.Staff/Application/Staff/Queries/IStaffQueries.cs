using ErrorOr;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Staff.Application.Staff.Queries;

public interface IStaffQueries
{
    Task<IReadOnlyCollection<GroomerListItemView>> ListGroomersAsync(CancellationToken cancellationToken);
    Task<GroomerDetailView?> GetGroomerAsync(Guid groomerId, CancellationToken cancellationToken);
    Task<ErrorOr<GroomerDetailView>> CreateGroomerAsync(string displayName, Guid? userId, CancellationToken cancellationToken);
    Task<ErrorOr<GroomerCapabilityView>> AddCapabilityAsync(AddGroomerCapabilityCommand command, CancellationToken cancellationToken);
    Task<ErrorOr<WorkingScheduleView>> UpsertWorkingScheduleAsync(Guid groomerId, int weekday, string startLocalTime, string endLocalTime, CancellationToken cancellationToken);
    Task<ErrorOr<TimeBlockView>> AddTimeBlockAsync(Guid groomerId, DateTime startAtUtc, DateTime endAtUtc, string reasonCode, string? notes, CancellationToken cancellationToken);
    Task<ErrorOr<GroomerScheduleView>> GetScheduleAsync(Guid groomerId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);
    Task<ErrorOr<GroomerAvailabilityCheckResult>> CheckAvailabilityAsync(CheckGroomerAvailabilityCommand command, CancellationToken cancellationToken);
}
