using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Booking.Infrastructure.Services;

public sealed class GroomerBookingQueries(
    AppDbContext dbContext,
    IGroomerProfileReadService groomerProfileReadService,
    IPetOperationalReadService petOperationalReadService,
    IVisitCatalogReadService visitCatalogReadService)
{
    public async Task<PagedResult<GroomerAppointmentListItemView>> ListAssignedAppointmentsAsync(Guid currentUserId, DateTime? fromUtc, DateTime? toUtc, int page, int pageSize, CancellationToken cancellationToken)
    {
        var groomer = await GetLinkedActiveGroomerAsync(currentUserId, cancellationToken);

        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize switch
        {
            <= 0 => 20,
            > 100 => 100,
            _ => pageSize
        };

        var query = dbContext.Set<Appointment>().Where(x => x.GroomerId == groomer.GroomerId);
        // Read filters preserve the legacy API convention: compare local/unspecified inputs as UTC wall-clock values.
        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.StartAtUtc >= DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc));
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.StartAtUtc < DateTime.SpecifyKind(toUtc.Value, DateTimeKind.Utc));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var appointments = await query
            .OrderBy(x => x.StartAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        var appointmentIds = appointments.Select(x => x.Id).ToArray();
        var items = await dbContext.Set<AppointmentItem>()
            .Where(x => appointmentIds.Contains(x.AppointmentId))
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var durationSnapshots = await dbContext.Set<DurationSnapshot>()
            .Where(x => items.Select(y => y.DurationSnapshotId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var petCache = new Dictionary<Guid, PetOperationalReadModel?>();
        var resultItems = new List<GroomerAppointmentListItemView>();
        foreach (var appointment in appointments)
        {
            if (!petCache.TryGetValue(appointment.PetId, out PetOperationalReadModel? value))
            {
                value = await petOperationalReadService.GetPetOperationalAsync(appointment.PetId, cancellationToken);
                petCache[appointment.PetId] = value;
            }

            var pet = value ?? throw new InvalidOperationException("Appointment pet does not exist.");
            var appointmentItems = items.Where(x => x.AppointmentId == appointment.Id).ToArray();

            resultItems.Add(new GroomerAppointmentListItemView(
                appointment.Id,
                pet.Id,
                pet.Name,
                pet.BreedName,
                appointment.StartAtUtc,
                appointment.EndAtUtc,
                appointment.Status,
                appointmentItems.Sum(x => durationSnapshots[x.DurationSnapshotId].ReservedMinutes),
                appointmentItems.Select(x => x.OfferDisplayNameSnapshot).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));
        }

        return new PagedResult<GroomerAppointmentListItemView>(resultItems, safePage, safePageSize, totalCount);
    }

    public async Task<GroomerAppointmentDetailView?> GetAssignedAppointmentAsync(Guid currentUserId, Guid appointmentId, CancellationToken cancellationToken)
    {
        var groomer = await GetLinkedActiveGroomerAsync(currentUserId, cancellationToken);
        var appointment = await dbContext.Set<Appointment>()
            .SingleOrDefaultAsync(x => x.Id == appointmentId && x.GroomerId == groomer.GroomerId, cancellationToken);

        if (appointment is null)
        {
            return null;
        }

        var pet = await petOperationalReadService.GetPetOperationalAsync(appointment.PetId, cancellationToken)
                  ?? throw new InvalidOperationException("Appointment pet does not exist.");

        var items = await dbContext.Set<AppointmentItem>()
            .Where(x => x.AppointmentId == appointment.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var durationSnapshots = await dbContext.Set<DurationSnapshot>()
            .Where(x => items.Select(y => y.DurationSnapshotId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var itemViews = new List<GroomerAppointmentItemView>();
        foreach (var item in items)
        {
            var expectedComponents = await visitCatalogReadService.GetIncludedComponentsAsync(item.OfferVersionId, cancellationToken);
            itemViews.Add(new GroomerAppointmentItemView(
                item.Id,
                item.ItemType,
                item.OfferId,
                item.OfferVersionId,
                item.OfferCodeSnapshot,
                item.OfferDisplayNameSnapshot,
                item.Quantity,
                durationSnapshots[item.DurationSnapshotId].ServiceMinutes,
                durationSnapshots[item.DurationSnapshotId].ReservedMinutes,
                expectedComponents.Select(x => x.ProcedureName).ToArray()));
        }

        return new GroomerAppointmentDetailView(
            appointment.Id,
            new GroomerAppointmentPetView(pet.Id, pet.Name, pet.AnimalTypeCode, pet.AnimalTypeName, pet.BreedName, pet.CoatTypeCode, pet.SizeCategoryCode),
            appointment.StartAtUtc,
            appointment.EndAtUtc,
            appointment.Status,
            itemViews.Sum(x => x.ReservedMinutes),
            string.IsNullOrWhiteSpace(pet.Notes) ? [] : [pet.Notes.Trim()],
            itemViews,
            appointment.CreatedAtUtc,
            appointment.UpdatedAtUtc);
    }

    private async Task<GroomerProfileReadModel> GetLinkedActiveGroomerAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var groomer = await groomerProfileReadService.GetByUserIdAsync(currentUserId, cancellationToken);
        if (groomer is null || !groomer.Active)
        {
            throw new UnauthorizedAccessException("Current user is not linked to an active groomer profile.");
        }

        return groomer;
    }
}
