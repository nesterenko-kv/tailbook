using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Contracts;
using Tailbook.Modules.Booking.Domain;

namespace Tailbook.Modules.Booking.Application;

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
        DateTime startAtUtc,
        DateTime endAtUtc,
        Guid? ignoredAppointmentId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Appointment>()
            .Where(x => x.GroomerId == groomerId)
            .Where(x => ActiveStatuses.Contains(x.Status))
            .Where(x => x.StartAtUtc < endAtUtc && x.EndAtUtc > startAtUtc);

        if (ignoredAppointmentId.HasValue)
        {
            query = query.Where(x => x.Id != ignoredAppointmentId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
