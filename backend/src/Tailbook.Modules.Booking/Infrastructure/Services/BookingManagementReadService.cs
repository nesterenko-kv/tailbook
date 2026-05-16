using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Search;

namespace Tailbook.Modules.Booking.Infrastructure.Services;

public sealed class BookingManagementReadService(
    AppDbContext dbContext,
    IPetQuoteProfileService petQuoteProfileService,
    IPetSummaryReadService petSummaryReadService,
    IGroomerProfileReadService groomerProfileReadService) : IBookingManagementReadService
{
    public async Task<PagedResult<BookingRequestListItemView>> ListBookingRequestsAsync(string? search, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize switch { <= 0 => 20, > 100 => 100, _ => pageSize };
        var searchTerms = SearchText.Terms(search);
        var petIdsBySearchTerm = await SearchPetIdsByTermAsync(searchTerms, cancellationToken);

        var query = dbContext.Set<BookingRequest>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(x => x.Status == normalizedStatus);
        }

        foreach (var term in searchTerms)
        {
            var matchingPetIds = petIdsBySearchTerm.GetValueOrDefault(term) ?? [];
            if (dbContext.Database.IsNpgsql())
            {
                var pattern = SearchText.LikePattern(term);
                query = query.Where(x =>
                    EF.Functions.ILike(x.Status, pattern, @"\") ||
                    EF.Functions.ILike(x.Channel, pattern, @"\") ||
                    (x.SelectionMode != null && EF.Functions.ILike(x.SelectionMode, pattern, @"\")) ||
                    (x.Notes != null && EF.Functions.ILike(x.Notes, pattern, @"\")) ||
                    (x.PetId.HasValue && matchingPetIds.Contains(x.PetId.Value)));
            }
            else
            {
                query = query.Where(x =>
                    x.Status.ToLower().Contains(term) ||
                    x.Channel.ToLower().Contains(term) ||
                    (x.SelectionMode != null && x.SelectionMode.ToLower().Contains(term)) ||
                    (x.Notes != null && x.Notes.ToLower().Contains(term)) ||
                    (x.PetId.HasValue && matchingPetIds.Contains(x.PetId.Value)));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        var requestIds = items.Select(x => x.Id).ToArray();
        var itemCounts = await dbContext.Set<BookingRequestItem>()
            .Where(x => requestIds.Contains(x.BookingRequestId))
            .GroupBy(x => x.BookingRequestId)
            .Select(x => new { BookingRequestId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.BookingRequestId, x => x.Count, cancellationToken);

        var summaries = await BuildBookingRequestSummariesAsync(items, cancellationToken);

        return new PagedResult<BookingRequestListItemView>(
            items.Select(x =>
            {
                var summary = summaries.GetValueOrDefault(x.Id);
                return new BookingRequestListItemView(
                    x.Id,
                    x.ClientId,
                    x.PetId,
                    x.RequestedByContactId,
                    x.PreferredGroomerId,
                    x.SelectionMode,
                    x.Channel,
                    x.Status,
                    itemCounts.GetValueOrDefault(x.Id, 0),
                    summary?.PetDisplayName,
                    summary?.RequesterDisplayName,
                    summary?.RequesterPrimaryContact,
                    summary?.PreferredGroomerName,
                    x.CreatedAt,
                    x.UpdatedAt);
            }).ToArray(),
            safePage,
            safePageSize,
            totalCount);
    }

    public async Task<BookingRequestDetailView?> GetBookingRequestAsync(Guid bookingRequestId, CancellationToken cancellationToken)
    {
        var bookingRequest = await dbContext.Set<BookingRequest>().SingleOrDefaultAsync(x => x.Id == bookingRequestId, cancellationToken);
        if (bookingRequest is null)
        {
            return null;
        }

        var items = await dbContext.Set<BookingRequestItem>()
            .Where(x => x.BookingRequestId == bookingRequestId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var subject = await BuildBookingRequestSummaryAsync(bookingRequest, cancellationToken);
        return new BookingRequestDetailView(
            bookingRequest.Id,
            bookingRequest.ClientId,
            bookingRequest.PetId,
            bookingRequest.RequestedByContactId,
            bookingRequest.PreferredGroomerId,
            subject?.PreferredGroomerName,
            bookingRequest.SelectionMode,
            bookingRequest.Channel,
            bookingRequest.Status,
            subject,
            DeserializePreferredTimes(bookingRequest.PreferredTimeJson),
            bookingRequest.Notes,
            items.Select(x => new BookingRequestItemView(x.Id, x.OfferId, x.OfferVersionId, x.ItemType, x.RequestedNotes)).ToArray(),
            bookingRequest.CreatedAt,
            bookingRequest.UpdatedAt);
    }

    public async Task<Guid?> GetAppointmentIdByBookingRequestAsync(Guid bookingRequestId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<Appointment>()
            .Where(x => x.BookingRequestId == bookingRequestId)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<AppointmentListItemView>> ListAppointmentsAsync(string? search, DateTimeOffset? from, DateTimeOffset? to, Guid? groomerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize switch { <= 0 => 20, > 100 => 100, _ => pageSize };
        var searchTerms = SearchText.Terms(search);
        var petIdsBySearchTerm = await SearchPetIdsByTermAsync(searchTerms, cancellationToken);

        var query = dbContext.Set<Appointment>().AsQueryable();
        if (from.HasValue)
        {
            query = query.Where(x => x.StartAt >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(x => x.StartAt < to.Value);
        }
        if (groomerId.HasValue)
        {
            query = query.Where(x => x.GroomerId == groomerId.Value);
        }

        foreach (var term in searchTerms)
        {
            var matchingPetIds = petIdsBySearchTerm.GetValueOrDefault(term) ?? [];
            if (dbContext.Database.IsNpgsql())
            {
                var pattern = SearchText.LikePattern(term);
                query = query.Where(appointment =>
                    EF.Functions.ILike(appointment.Status, pattern, @"\") ||
                    (appointment.CancellationReasonCode != null && EF.Functions.ILike(appointment.CancellationReasonCode, pattern, @"\")) ||
                    (appointment.CancellationNotes != null && EF.Functions.ILike(appointment.CancellationNotes, pattern, @"\")) ||
                    matchingPetIds.Contains(appointment.PetId) ||
                    dbContext.Set<AppointmentItem>().Any(item =>
                        item.AppointmentId == appointment.Id &&
                        (EF.Functions.ILike(item.ItemType, pattern, @"\") ||
                         EF.Functions.ILike(item.OfferCodeSnapshot, pattern, @"\") ||
                         EF.Functions.ILike(item.OfferDisplayNameSnapshot, pattern, @"\"))));
            }
            else
            {
                query = query.Where(appointment =>
                    appointment.Status.ToLower().Contains(term) ||
                    (appointment.CancellationReasonCode != null && appointment.CancellationReasonCode.ToLower().Contains(term)) ||
                    (appointment.CancellationNotes != null && appointment.CancellationNotes.ToLower().Contains(term)) ||
                    matchingPetIds.Contains(appointment.PetId) ||
                    dbContext.Set<AppointmentItem>().Any(item =>
                        item.AppointmentId == appointment.Id &&
                        (item.ItemType.ToLower().Contains(term) ||
                         item.OfferCodeSnapshot.ToLower().Contains(term) ||
                         item.OfferDisplayNameSnapshot.ToLower().Contains(term))));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var appointments = await query
            .OrderBy(x => x.StartAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        var appointmentIds = appointments.Select(x => x.Id).ToArray();
        var itemCounts = await dbContext.Set<AppointmentItem>()
            .Where(x => appointmentIds.Contains(x.AppointmentId))
            .GroupBy(x => x.AppointmentId)
            .Select(x => new { AppointmentId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.AppointmentId, x => x.Count, cancellationToken);

        var totals = await GetAppointmentTotalsAsync(appointmentIds, cancellationToken);

        return new PagedResult<AppointmentListItemView>(
            appointments.Select(x => new AppointmentListItemView(
                x.Id,
                x.BookingRequestId,
                x.PetId,
                x.GroomerId,
                x.StartAt,
                x.EndAt,
                x.Status,
                x.VersionNo,
                itemCounts.GetValueOrDefault(x.Id, 0),
                totals.GetValueOrDefault(x.Id, 0m))).ToArray(),
            safePage,
            safePageSize,
            totalCount);
    }

    public async Task<AppointmentDetailView?> GetAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.Set<Appointment>().SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);
        if (appointment is null)
        {
            return null;
        }

        var items = await dbContext.Set<AppointmentItem>()
            .Where(x => x.AppointmentId == appointmentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var priceSnapshots = await dbContext.Set<PriceSnapshot>()
            .Where(x => items.Select(y => y.PriceSnapshotId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var durationSnapshots = await dbContext.Set<DurationSnapshot>()
            .Where(x => items.Select(y => y.DurationSnapshotId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var pet = await petQuoteProfileService.GetPetAsync(appointment.PetId, cancellationToken);
        if (pet is null)
        {
            return null;
        }

        return new AppointmentDetailView(
            appointment.Id,
            appointment.BookingRequestId,
            new AppointmentPetView(appointment.PetId, pet.ClientId, pet.AnimalTypeCode, pet.AnimalTypeName, pet.BreedName),
            appointment.GroomerId,
            appointment.StartAt,
            appointment.EndAt,
            appointment.Status,
            appointment.VersionNo,
            items.Select(x => new AppointmentItemView(
                x.Id,
                x.ItemType,
                x.OfferId,
                x.OfferVersionId,
                x.OfferCodeSnapshot,
                x.OfferDisplayNameSnapshot,
                x.Quantity,
                x.PriceSnapshotId,
                x.DurationSnapshotId,
                priceSnapshots.GetValueOrDefault(x.PriceSnapshotId)?.TotalAmount ?? 0m,
                durationSnapshots.GetValueOrDefault(x.DurationSnapshotId)?.ServiceMinutes ?? 0,
                durationSnapshots.GetValueOrDefault(x.DurationSnapshotId)?.ReservedMinutes ?? 0)).ToArray(),
            items.Sum(x => priceSnapshots.GetValueOrDefault(x.PriceSnapshotId)?.TotalAmount ?? 0m),
            items.Sum(x => durationSnapshots.GetValueOrDefault(x.DurationSnapshotId)?.ServiceMinutes ?? 0),
            appointment.EndAt > appointment.StartAt
                ? (int)(appointment.EndAt - appointment.StartAt).TotalMinutes
                : items.Sum(x => durationSnapshots.GetValueOrDefault(x.DurationSnapshotId)?.ReservedMinutes ?? 0),
            appointment.CancellationReasonCode,
            appointment.CancellationNotes,
            appointment.CancelledAt,
            appointment.CreatedAt,
            appointment.UpdatedAt);
    }

    private async Task<Dictionary<Guid, BookingRequestSubjectView>> BuildBookingRequestSummariesAsync(IReadOnlyCollection<BookingRequest> bookingRequests, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, BookingRequestSubjectView>();
        foreach (var request in bookingRequests)
        {
            var summary = await BuildBookingRequestSummaryAsync(request, cancellationToken);
            if (summary is not null)
            {
                result[request.Id] = summary;
            }
        }

        return result;
    }

    private async Task<BookingRequestSubjectView?> BuildBookingRequestSummaryAsync(BookingRequest request, CancellationToken cancellationToken)
    {
        var guestIntake = DeserializeGuestIntake(request.GuestIntakeJson);
        string? petDisplayName = guestIntake?.Pet?.DisplayName;
        string? animalTypeCode = guestIntake?.Pet?.AnimalTypeCode;
        string? breedName = guestIntake?.Pet?.BreedName;
        string? requesterDisplayName = guestIntake?.Requester?.DisplayName;
        string? requesterPrimaryContact = guestIntake?.Requester?.PrimaryContactDisplay;

        if (request.PetId.HasValue)
        {
            var petSummary = await petSummaryReadService.GetPetSummaryAsync(request.PetId.Value, cancellationToken);
            if (petSummary is not null)
            {
                petDisplayName = petSummary.Name;
                animalTypeCode = petSummary.AnimalTypeCode;
                breedName = petSummary.BreedName;
            }
        }

        string? preferredGroomerName = null;
        if (request.PreferredGroomerId.HasValue)
        {
            preferredGroomerName = (await groomerProfileReadService.GetByGroomerIdAsync(request.PreferredGroomerId.Value, cancellationToken))?.DisplayName;
        }

        return new BookingRequestSubjectView(
            petDisplayName,
            animalTypeCode,
            breedName,
            requesterDisplayName,
            requesterPrimaryContact,
            preferredGroomerName,
            guestIntake);
    }

    private static IReadOnlyCollection<PreferredTimeWindowView> DeserializePreferredTimes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<PreferredTimeWindowView[]>(json) ?? [];
    }

    private static GuestBookingIntakeView? DeserializeGuestIntake(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<GuestBookingIntakeView>(json);
    }

    private async Task<Dictionary<Guid, decimal>> GetAppointmentTotalsAsync(Guid[] appointmentIds, CancellationToken cancellationToken)
    {
        var items = await dbContext.Set<AppointmentItem>()
            .Where(x => appointmentIds.Contains(x.AppointmentId))
            .ToListAsync(cancellationToken);

        var snapshotIds = items.Select(x => x.PriceSnapshotId).Distinct().ToArray();
        var snapshots = await dbContext.Set<PriceSnapshot>()
            .Where(x => snapshotIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.TotalAmount, cancellationToken);

        return items.GroupBy(x => x.AppointmentId)
            .ToDictionary(x => x.Key, x => x.Sum(y => snapshots.GetValueOrDefault(y.PriceSnapshotId)));
    }

    private async Task<Dictionary<string, Guid[]>> SearchPetIdsByTermAsync(string[] searchTerms, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, Guid[]>(StringComparer.Ordinal);
        foreach (var term in searchTerms)
        {
            result[term] = (await petSummaryReadService.SearchPetIdsAsync(term, 1000, cancellationToken)).ToArray();
        }

        return result;
    }
}
