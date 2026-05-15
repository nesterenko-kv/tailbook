using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Booking.Infrastructure.Services;

public sealed class BookingOverlapReadService(AppDbContext dbContext) : IAppointmentOverlapReadService
{
    private static readonly IEnumerable<string> ActiveStatuses =
    [
        AppointmentStatusCodes.Confirmed,
        AppointmentStatusCodes.Rescheduled,
        AppointmentStatusCodes.CheckedIn,
        AppointmentStatusCodes.InProgress,
        AppointmentStatusCodes.Completed
    ];

    public async Task<bool> HasOverlapAsync(
        Guid groomerId,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        Guid? ignoredAppointmentId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Appointment>()
            .AsNoTracking()
            .Where(x => x.GroomerId == groomerId)
            .Where(x => ActiveStatuses.Contains(x.Status))
            .Where(x => x.StartAt < endAt && x.EndAt > startAt);

        if (ignoredAppointmentId.HasValue)
        {
            query = query.Where(x => x.Id != ignoredAppointmentId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AppointmentBusyIntervalReadModel>> ListBusyIntervalsAsync(
        Guid groomerId,
        DateTimeOffset from,
        DateTimeOffset to,
        Guid? ignoredAppointmentId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Appointment>()
            .AsNoTracking()
            .Where(x => x.GroomerId == groomerId)
            .Where(x => ActiveStatuses.Contains(x.Status))
            .Where(x => x.StartAt < to && x.EndAt > from);

        if (ignoredAppointmentId.HasValue)
        {
            query = query.Where(x => x.Id != ignoredAppointmentId.Value);
        }

        return await query
            .OrderBy(x => x.StartAt)
            .Select(x => new AppointmentBusyIntervalReadModel(x.Id, x.StartAt, x.EndAt))
            .ToArrayAsync(cancellationToken);
    }
}
