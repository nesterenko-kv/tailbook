using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Contracts;
using Tailbook.Modules.Booking.Domain;

namespace Tailbook.Modules.Booking.Application;

public sealed class BookingManagementQueries(
    AppDbContext dbContext,
    BookingSnapshotComposer bookingSnapshotComposer,
    IPetQuoteProfileService petQuoteProfileService,
    IPetSummaryReadService petSummaryReadService,
    IClientReferenceValidationService clientReferenceValidationService,
    IContactReferenceValidationService contactReferenceValidationService,
    IOfferReferenceValidationService offerReferenceValidationService,
    IStaffSchedulingService staffSchedulingService,
    IGroomerProfileReadService groomerProfileReadService,
    IAuditTrailService auditTrailService,
    IOutboxPublisher outboxPublisher)
{
    public async Task<BookingRequestDetailView> CreateBookingRequestAsync(CreateBookingRequestCommand command, string? actorUserId, CancellationToken cancellationToken)
    {
        PetQuoteProfile? pet = null;
        if (command.PetId.HasValue)
        {
            pet = await petQuoteProfileService.GetPetAsync(command.PetId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Pet does not exist.");
        }
        else if (command.GuestIntake is null)
        {
            throw new InvalidOperationException("Booking request must reference a saved pet or include guest pet details.");
        }

        if (command.ClientId.HasValue)
        {
            var clientExists = await clientReferenceValidationService.ExistsAsync(command.ClientId.Value, cancellationToken);
            if (!clientExists)
            {
                throw new InvalidOperationException("Client does not exist.");
            }

            if (pet?.ClientId.HasValue == true && pet.ClientId.Value != command.ClientId.Value)
            {
                throw new InvalidOperationException("Selected pet does not belong to the specified client.");
            }
        }

        if (command.RequestedByContactId.HasValue)
        {
            var contactExists = await contactReferenceValidationService.ExistsAsync(command.RequestedByContactId.Value, cancellationToken);
            if (!contactExists)
            {
                throw new InvalidOperationException("Requested-by contact does not exist.");
            }
        }

        foreach (var item in command.Items)
        {
            var exists = await offerReferenceValidationService.ExistsAsync(item.OfferId, cancellationToken);
            if (!exists)
            {
                throw new InvalidOperationException("One or more requested offers do not exist.");
            }
        }

        var utcNow = DateTime.UtcNow;
        var entity = new BookingRequest
        {
            Id = Guid.NewGuid(),
            ClientId = command.ClientId ?? pet?.ClientId,
            PetId = command.PetId,
            RequestedByContactId = command.RequestedByContactId,
            PreferredGroomerId = command.PreferredGroomerId,
            Channel = string.IsNullOrWhiteSpace(command.Channel) ? BookingChannelCodes.Admin : command.Channel.Trim(),
            Status = NormalizeStatus(command.InitialStatus, command.PetId.HasValue ? BookingRequestStatusCodes.Submitted : BookingRequestStatusCodes.NeedsReview),
            SelectionMode = NormalizeSelectionMode(command.SelectionMode),
            GuestIntakeJson = SerializeGuestIntake(command.GuestIntake),
            PreferredTimeJson = SerializePreferredTimes(command.PreferredTimes),
            Notes = NormalizeOptional(command.Notes),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        dbContext.Set<BookingRequest>().Add(entity);
        dbContext.Set<BookingRequestItem>().AddRange(command.Items.Select(x => new BookingRequestItem
        {
            Id = Guid.NewGuid(),
            BookingRequestId = entity.Id,
            OfferId = x.OfferId,
            OfferVersionId = null,
            ItemType = NormalizeOptional(x.ItemType),
            RequestedNotes = NormalizeOptional(x.RequestedNotes),
            CreatedAtUtc = utcNow
        }));

        await outboxPublisher.PublishAsync("booking", "BookingRequested", new
        {
            bookingRequestId = entity.Id,
            petId = entity.PetId,
            clientId = entity.ClientId,
            channel = entity.Channel,
            status = entity.Status,
            selectionMode = entity.SelectionMode
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync("booking", "booking_request", entity.Id.ToString("D"), "CREATE", ParseGuid(actorUserId), null,
            JsonSerializer.Serialize(new { entity.Status, entity.Channel, entity.SelectionMode, entity.PetId, entity.ClientId }), cancellationToken);

        return (await GetBookingRequestAsync(entity.Id, cancellationToken))!;
    }

    public async Task<BookingRequestDetailView?> AttachBookingRequestContextAsync(AttachBookingRequestContextCommand command, string? actorUserId, CancellationToken cancellationToken)
    {
        var bookingRequest = await dbContext.Set<BookingRequest>()
            .SingleOrDefaultAsync(x => x.Id == command.BookingRequestId, cancellationToken);
        if (bookingRequest is null)
        {
            return null;
        }

        if (bookingRequest.Status == BookingRequestStatusCodes.Converted)
        {
            throw new InvalidOperationException("Converted booking requests cannot be relinked.");
        }

        var pet = await petQuoteProfileService.GetPetAsync(command.PetId, cancellationToken)
            ?? throw new InvalidOperationException("Pet does not exist.");

        if (command.ClientId.HasValue)
        {
            var clientExists = await clientReferenceValidationService.ExistsAsync(command.ClientId.Value, cancellationToken);
            if (!clientExists)
            {
                throw new InvalidOperationException("Client does not exist.");
            }

            if (pet.ClientId.HasValue && pet.ClientId.Value != command.ClientId.Value)
            {
                throw new InvalidOperationException("Selected pet does not belong to the specified client.");
            }
        }

        if (command.RequestedByContactId.HasValue)
        {
            var contactExists = await contactReferenceValidationService.ExistsAsync(command.RequestedByContactId.Value, cancellationToken);
            if (!contactExists)
            {
                throw new InvalidOperationException("Requested-by contact does not exist.");
            }
        }

        bookingRequest.ClientId = command.ClientId ?? pet.ClientId;
        bookingRequest.PetId = command.PetId;
        bookingRequest.RequestedByContactId = command.RequestedByContactId;
        bookingRequest.UpdatedAtUtc = DateTime.UtcNow;

        if (bookingRequest.Status == BookingRequestStatusCodes.NeedsReview)
        {
            bookingRequest.Status = BookingRequestStatusCodes.Submitted;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync("booking", "booking_request", bookingRequest.Id.ToString("D"), "ATTACH_CONTEXT", ParseGuid(actorUserId), null,
            JsonSerializer.Serialize(new { bookingRequest.ClientId, bookingRequest.PetId, bookingRequest.RequestedByContactId, bookingRequest.Status }), cancellationToken);

        return await GetBookingRequestAsync(bookingRequest.Id, cancellationToken);
    }

    public async Task<PagedResult<BookingRequestListItemView>> ListBookingRequestsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize switch { <= 0 => 20, > 100 => 100, _ => pageSize };

        var query = dbContext.Set<BookingRequest>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(x => x.Status == normalizedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
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
                    x.CreatedAtUtc,
                    x.UpdatedAtUtc);
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
            .OrderBy(x => x.CreatedAtUtc)
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
            bookingRequest.CreatedAtUtc,
            bookingRequest.UpdatedAtUtc);
    }

    public async Task<AppointmentDetailView> ConvertBookingRequestToAppointmentAsync(ConvertBookingRequestToAppointmentCommand command, string? actorUserId, CancellationToken cancellationToken)
    {
        var bookingRequest = await dbContext.Set<BookingRequest>().SingleOrDefaultAsync(x => x.Id == command.BookingRequestId, cancellationToken)
            ?? throw new InvalidOperationException("Booking request does not exist.");

        if (bookingRequest.Status == BookingRequestStatusCodes.Converted)
        {
            throw new InvalidOperationException("Booking request has already been converted.");
        }

        if (!bookingRequest.PetId.HasValue)
        {
            throw new InvalidOperationException("Guest booking requests must be linked to a saved pet before conversion to an appointment.");
        }

        var requestItems = await dbContext.Set<BookingRequestItem>()
            .Where(x => x.BookingRequestId == bookingRequest.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (requestItems.Count == 0)
        {
            throw new InvalidOperationException("Booking request must contain at least one requested item.");
        }

        var result = await CreateAppointmentInternalAsync(
            bookingRequest.Id,
            bookingRequest.PetId.Value,
            command.GroomerId,
            command.StartAtUtc,
            requestItems.Select(x => new PreviewQuoteItemCommand(x.OfferId, x.ItemType)).ToArray(),
            actorUserId,
            cancellationToken);

        bookingRequest.Status = BookingRequestStatusCodes.Converted;
        bookingRequest.UpdatedAtUtc = DateTime.UtcNow;
        await outboxPublisher.PublishAsync("booking", "BookingRequestConverted", new
        {
            bookingRequestId = bookingRequest.Id,
            appointmentId = result.Id
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync("booking", "booking_request", bookingRequest.Id.ToString("D"), "CONVERT_TO_APPOINTMENT", ParseGuid(actorUserId), null, JsonSerializer.Serialize(new { appointmentId = result.Id }), cancellationToken);

        return result;
    }

    public Task<AppointmentDetailView> CreateAppointmentAsync(CreateAppointmentCommand command, string? actorUserId, CancellationToken cancellationToken)
    {
        return CreateAppointmentInternalAsync(
            null,
            command.PetId,
            command.GroomerId,
            command.StartAtUtc,
            command.Items.Select(x => new PreviewQuoteItemCommand(x.OfferId, x.ItemType)).ToArray(),
            actorUserId,
            cancellationToken);
    }

    private async Task<AppointmentDetailView> CreateAppointmentInternalAsync(
        Guid? bookingRequestId,
        Guid petId,
        Guid groomerId,
        DateTime startAtUtc,
        IReadOnlyCollection<PreviewQuoteItemCommand> items,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var composition = await bookingSnapshotComposer.ComposeAppointmentAsync(
            petId,
            groomerId,
            DateTime.SpecifyKind(startAtUtc, DateTimeKind.Utc),
            items,
            actorUserId,
            cancellationToken);

        var utcNow = DateTime.UtcNow;
        var actorGuid = ParseGuid(actorUserId);
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            BookingRequestId = bookingRequestId,
            PetId = petId,
            GroomerId = groomerId,
            StartAtUtc = composition.StartAtUtc,
            EndAtUtc = composition.EndAtUtc,
            Status = AppointmentStatusCodes.Confirmed,
            VersionNo = 1,
            CreatedByUserId = actorGuid,
            UpdatedByUserId = actorGuid,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        dbContext.Set<Appointment>().Add(appointment);
        dbContext.Set<AppointmentItem>().AddRange(composition.Items.Select(x => new AppointmentItem
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointment.Id,
            ItemType = x.OfferType,
            OfferId = x.OfferId,
            OfferVersionId = x.OfferVersionId,
            OfferCodeSnapshot = x.OfferCode,
            OfferDisplayNameSnapshot = x.DisplayName,
            Quantity = 1,
            PriceSnapshotId = x.PriceSnapshot.Id,
            DurationSnapshotId = x.DurationSnapshot.Id,
            CreatedAtUtc = utcNow
        }));

        await outboxPublisher.PublishAsync("booking", "AppointmentCreated", new
        {
            appointmentId = appointment.Id,
            bookingRequestId = appointment.BookingRequestId,
            petId = appointment.PetId,
            groomerId = appointment.GroomerId,
            startAtUtc = appointment.StartAtUtc,
            endAtUtc = appointment.EndAtUtc
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync("booking", "appointment", appointment.Id.ToString("D"), "CREATE", actorGuid, null, JsonSerializer.Serialize(new { appointment.Status, appointment.VersionNo }), cancellationToken);
        return (await GetAppointmentAsync(appointment.Id, cancellationToken))!;
    }

    public async Task<PagedResult<AppointmentListItemView>> ListAppointmentsAsync(DateTime? fromUtc, DateTime? toUtc, Guid? groomerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize switch { <= 0 => 20, > 100 => 100, _ => pageSize };

        var query = dbContext.Set<Appointment>().AsQueryable();
        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.StartAtUtc >= fromUtc.Value);
        }
        if (toUtc.HasValue)
        {
            query = query.Where(x => x.StartAtUtc < toUtc.Value);
        }
        if (groomerId.HasValue)
        {
            query = query.Where(x => x.GroomerId == groomerId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var appointments = await query
            .OrderBy(x => x.StartAtUtc)
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
                x.StartAtUtc,
                x.EndAtUtc,
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
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var priceSnapshots = await dbContext.Set<PriceSnapshot>()
            .Where(x => items.Select(y => y.PriceSnapshotId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var durationSnapshots = await dbContext.Set<DurationSnapshot>()
            .Where(x => items.Select(y => y.DurationSnapshotId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var pet = await petQuoteProfileService.GetPetAsync(appointment.PetId, cancellationToken)
            ?? throw new InvalidOperationException("Pet does not exist.");

        return new AppointmentDetailView(
            appointment.Id,
            appointment.BookingRequestId,
            new AppointmentPetView(appointment.PetId, pet.ClientId, pet.AnimalTypeCode, pet.AnimalTypeName, pet.BreedName),
            appointment.GroomerId,
            appointment.StartAtUtc,
            appointment.EndAtUtc,
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
                priceSnapshots[x.PriceSnapshotId].TotalAmount,
                durationSnapshots[x.DurationSnapshotId].ServiceMinutes,
                durationSnapshots[x.DurationSnapshotId].ReservedMinutes)).ToArray(),
            items.Sum(x => priceSnapshots[x.PriceSnapshotId].TotalAmount),
            items.Sum(x => durationSnapshots[x.DurationSnapshotId].ServiceMinutes),
            appointment.EndAtUtc > appointment.StartAtUtc ? (int)(appointment.EndAtUtc - appointment.StartAtUtc).TotalMinutes : items.Sum(x => durationSnapshots[x.DurationSnapshotId].ReservedMinutes),
            appointment.CancellationReasonCode,
            appointment.CancellationNotes,
            appointment.CancelledAtUtc,
            appointment.CreatedAtUtc,
            appointment.UpdatedAtUtc);
    }

    public async Task<AppointmentDetailView?> RescheduleAppointmentAsync(RescheduleAppointmentCommand command, string? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.Set<Appointment>().SingleOrDefaultAsync(x => x.Id == command.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return null;
        }

        EnsureVersion(appointment, command.ExpectedVersionNo);
        EnsureMutable(appointment);

        var items = await dbContext.Set<AppointmentItem>()
            .Where(x => x.AppointmentId == appointment.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var durationSnapshots = await dbContext.Set<DurationSnapshot>()
            .Where(x => items.Select(y => y.DurationSnapshotId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var modifierLines = await dbContext.Set<DurationSnapshotLine>()
            .Where(x => items.Select(y => y.DurationSnapshotId).Contains(x.DurationSnapshotId))
            .Where(x => x.LineType == "GroomerCapabilityModifier")
            .ToListAsync(cancellationToken);

        var baseReservedMinutes = items.Sum(x =>
        {
            var snapshot = durationSnapshots[x.DurationSnapshotId];
            var modifierTotal = modifierLines
                .Where(y => y.DurationSnapshotId == x.DurationSnapshotId)
                .Sum(y => y.Minutes);
            return snapshot.ReservedMinutes - modifierTotal;
        });

        var availability = await staffSchedulingService.CheckAvailabilityAsync(
            command.GroomerId,
            appointment.PetId,
            items.Select(x => x.OfferId).ToArray(),
            DateTime.SpecifyKind(command.StartAtUtc, DateTimeKind.Utc),
            baseReservedMinutes,
            appointment.Id,
            cancellationToken);

        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException(string.Join(" ", availability.Reasons));
        }

        appointment.GroomerId = command.GroomerId;
        appointment.StartAtUtc = DateTime.SpecifyKind(command.StartAtUtc, DateTimeKind.Utc);
        appointment.EndAtUtc = availability.EndAtUtc;
        appointment.Status = AppointmentStatusCodes.Rescheduled;
        appointment.VersionNo += 1;
        appointment.UpdatedAtUtc = DateTime.UtcNow;
        appointment.UpdatedByUserId = ParseGuid(actorUserId);

        await outboxPublisher.PublishAsync("booking", "AppointmentRescheduled", new
        {
            appointmentId = appointment.Id,
            groomerId = appointment.GroomerId,
            startAtUtc = appointment.StartAtUtc,
            endAtUtc = appointment.EndAtUtc,
            versionNo = appointment.VersionNo
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync("booking", "appointment", appointment.Id.ToString("D"), "RESCHEDULE", ParseGuid(actorUserId), null, JsonSerializer.Serialize(new { appointment.VersionNo, appointment.StartAtUtc, appointment.EndAtUtc }), cancellationToken);
        return await GetAppointmentAsync(appointment.Id, cancellationToken);
    }

    public async Task<AppointmentDetailView?> CancelAppointmentAsync(CancelAppointmentCommand command, string? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.Set<Appointment>().SingleOrDefaultAsync(x => x.Id == command.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return null;
        }

        EnsureVersion(appointment, command.ExpectedVersionNo);
        EnsureMutable(appointment);

        appointment.Status = AppointmentStatusCodes.Cancelled;
        appointment.CancellationReasonCode = NormalizeReasonCode(command.ReasonCode);
        appointment.CancellationNotes = NormalizeOptional(command.Notes);
        appointment.CancelledAtUtc = DateTime.UtcNow;
        appointment.VersionNo += 1;
        appointment.UpdatedAtUtc = DateTime.UtcNow;
        appointment.UpdatedByUserId = ParseGuid(actorUserId);

        await outboxPublisher.PublishAsync("booking", "AppointmentCancelled", new
        {
            appointmentId = appointment.Id,
            reasonCode = appointment.CancellationReasonCode,
            versionNo = appointment.VersionNo
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync("booking", "appointment", appointment.Id.ToString("D"), "CANCEL", ParseGuid(actorUserId), null, JsonSerializer.Serialize(new { appointment.CancellationReasonCode, appointment.VersionNo }), cancellationToken);
        return await GetAppointmentAsync(appointment.Id, cancellationToken);
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

    private static void EnsureVersion(Appointment appointment, int expectedVersionNo)
    {
        if (appointment.VersionNo != expectedVersionNo)
        {
            throw new BookingConcurrencyException($"Appointment version mismatch. Expected {expectedVersionNo}, actual {appointment.VersionNo}.");
        }
    }

    private static void EnsureMutable(Appointment appointment)
    {
        if (appointment.Status == AppointmentStatusCodes.Cancelled || appointment.Status == AppointmentStatusCodes.Closed)
        {
            throw new InvalidOperationException("Appointment is not mutable in its current status.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeReasonCode(string reasonCode)
    {
        var normalized = reasonCode.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Cancellation reason code is required.");
        }

        return normalized.ToUpperInvariant();
    }

    private static string NormalizeStatus(string? status, string fallback)
    {
        var normalized = NormalizeOptional(status);
        return normalized switch
        {
            null => fallback,
            _ when BookingRequestStatusCodes.All.Contains(normalized, StringComparer.OrdinalIgnoreCase) =>
                BookingRequestStatusCodes.All.Single(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)),
            _ => throw new InvalidOperationException($"Unknown booking request status '{status}'.")
        };
    }

    private static string? NormalizeSelectionMode(string? selectionMode)
    {
        var normalized = NormalizeOptional(selectionMode);
        if (normalized is null)
        {
            return null;
        }

        return normalized switch
        {
            _ when string.Equals(normalized, BookingRequestSelectionModeCodes.AnySuitableGroomer, StringComparison.OrdinalIgnoreCase) => BookingRequestSelectionModeCodes.AnySuitableGroomer,
            _ when string.Equals(normalized, BookingRequestSelectionModeCodes.SpecificGroomer, StringComparison.OrdinalIgnoreCase) => BookingRequestSelectionModeCodes.SpecificGroomer,
            _ when string.Equals(normalized, BookingRequestSelectionModeCodes.ExactSlot, StringComparison.OrdinalIgnoreCase) => BookingRequestSelectionModeCodes.ExactSlot,
            _ when string.Equals(normalized, BookingRequestSelectionModeCodes.PreferredWindow, StringComparison.OrdinalIgnoreCase) => BookingRequestSelectionModeCodes.PreferredWindow,
            _ => throw new InvalidOperationException($"Unknown selection mode '{selectionMode}'.")
        };
    }

    private static string? SerializePreferredTimes(IReadOnlyCollection<PreferredTimeWindowCommand> windows)
    {
        if (windows.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(windows.Select(x => new PreferredTimeWindowView(
            DateTime.SpecifyKind(x.StartAtUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(x.EndAtUtc, DateTimeKind.Utc),
            NormalizeOptional(x.Label))).ToArray());
    }

    private static IReadOnlyCollection<PreferredTimeWindowView> DeserializePreferredTimes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<PreferredTimeWindowView[]>(json) ?? [];
    }

    private static string? SerializeGuestIntake(GuestBookingIntakeCommand? guestIntake)
    {
        if (guestIntake is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(new GuestBookingIntakeView(
            guestIntake.Requester is null
                ? null
                : new BookingRequesterSnapshotView(
                    NormalizeOptional(guestIntake.Requester.DisplayName),
                    NormalizeOptional(guestIntake.Requester.Phone),
                    NormalizeOptional(guestIntake.Requester.InstagramHandle),
                    NormalizeOptional(guestIntake.Requester.Email),
                    NormalizeOptional(guestIntake.Requester.PreferredContactMethodCode)),
            guestIntake.Pet is null
                ? null
                : new BookingGuestPetSnapshotView(
                    NormalizeOptional(guestIntake.Pet.DisplayName),
                    guestIntake.Pet.AnimalTypeId,
                    NormalizeOptional(guestIntake.Pet.AnimalTypeCode),
                    NormalizeOptional(guestIntake.Pet.AnimalTypeName),
                    guestIntake.Pet.BreedId,
                    NormalizeOptional(guestIntake.Pet.BreedCode),
                    NormalizeOptional(guestIntake.Pet.BreedName),
                    guestIntake.Pet.CoatTypeId,
                    NormalizeOptional(guestIntake.Pet.CoatTypeCode),
                    NormalizeOptional(guestIntake.Pet.CoatTypeName),
                    guestIntake.Pet.SizeCategoryId,
                    NormalizeOptional(guestIntake.Pet.SizeCategoryCode),
                    NormalizeOptional(guestIntake.Pet.SizeCategoryName),
                    guestIntake.Pet.WeightKg,
                    NormalizeOptional(guestIntake.Pet.Notes))));
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

    private static Guid? ParseGuid(string? value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}

public sealed record CreateBookingRequestCommand(
    Guid? ClientId,
    Guid? PetId,
    Guid? RequestedByContactId,
    string? Channel,
    string? Notes,
    IReadOnlyCollection<PreferredTimeWindowCommand> PreferredTimes,
    IReadOnlyCollection<CreateBookingRequestItemCommand> Items,
    Guid? PreferredGroomerId = null,
    string? SelectionMode = null,
    GuestBookingIntakeCommand? GuestIntake = null,
    string? InitialStatus = null);

public sealed record AttachBookingRequestContextCommand(
    Guid BookingRequestId,
    Guid? ClientId,
    Guid PetId,
    Guid? RequestedByContactId);

public sealed record GuestBookingIntakeCommand(
    GuestBookingRequesterCommand? Requester,
    GuestBookingPetCommand? Pet);

public sealed record GuestBookingRequesterCommand(
    string? DisplayName,
    string? Phone,
    string? InstagramHandle,
    string? Email,
    string? PreferredContactMethodCode);

public sealed record GuestBookingPetCommand(
    string? DisplayName,
    Guid? AnimalTypeId,
    string? AnimalTypeCode,
    string? AnimalTypeName,
    Guid? BreedId,
    string? BreedCode,
    string? BreedName,
    Guid? CoatTypeId,
    string? CoatTypeCode,
    string? CoatTypeName,
    Guid? SizeCategoryId,
    string? SizeCategoryCode,
    string? SizeCategoryName,
    decimal? WeightKg,
    string? Notes);

public sealed record PreferredTimeWindowCommand(DateTime StartAtUtc, DateTime EndAtUtc, string? Label);
public sealed record CreateBookingRequestItemCommand(Guid OfferId, string? ItemType, string? RequestedNotes);
public sealed record ConvertBookingRequestToAppointmentCommand(Guid BookingRequestId, Guid GroomerId, DateTime StartAtUtc);
public sealed record CreateAppointmentCommand(Guid PetId, Guid GroomerId, DateTime StartAtUtc, IReadOnlyCollection<CreateAppointmentItemCommand> Items);
public sealed record CreateAppointmentItemCommand(Guid OfferId, string? ItemType);
public sealed record RescheduleAppointmentCommand(Guid AppointmentId, Guid GroomerId, DateTime StartAtUtc, int ExpectedVersionNo);
public sealed record CancelAppointmentCommand(Guid AppointmentId, int ExpectedVersionNo, string ReasonCode, string? Notes);

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
public sealed record PreferredTimeWindowView(DateTime StartAtUtc, DateTime EndAtUtc, string? Label);
public sealed record BookingRequestItemView(Guid Id, Guid OfferId, Guid? OfferVersionId, string? ItemType, string? RequestedNotes);
public sealed record BookingRequestListItemView(
    Guid Id,
    Guid? ClientId,
    Guid? PetId,
    Guid? RequestedByContactId,
    Guid? PreferredGroomerId,
    string? SelectionMode,
    string Channel,
    string Status,
    int ItemCount,
    string? PetDisplayName,
    string? RequesterDisplayName,
    string? RequesterPrimaryContact,
    string? PreferredGroomerName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record BookingRequestDetailView(
    Guid Id,
    Guid? ClientId,
    Guid? PetId,
    Guid? RequestedByContactId,
    Guid? PreferredGroomerId,
    string? PreferredGroomerName,
    string? SelectionMode,
    string Channel,
    string Status,
    BookingRequestSubjectView? Subject,
    IReadOnlyCollection<PreferredTimeWindowView> PreferredTimes,
    string? Notes,
    IReadOnlyCollection<BookingRequestItemView> Items,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record BookingRequestSubjectView(
    string? PetDisplayName,
    string? AnimalTypeCode,
    string? BreedName,
    string? RequesterDisplayName,
    string? RequesterPrimaryContact,
    string? PreferredGroomerName,
    GuestBookingIntakeView? GuestIntake);

public sealed record GuestBookingIntakeView(
    BookingRequesterSnapshotView? Requester,
    BookingGuestPetSnapshotView? Pet);

public sealed record BookingRequesterSnapshotView(
    string? DisplayName,
    string? Phone,
    string? InstagramHandle,
    string? Email,
    string? PreferredContactMethodCode)
{
    public string? PrimaryContactDisplay =>
        !string.IsNullOrWhiteSpace(Phone) ? Phone :
        !string.IsNullOrWhiteSpace(InstagramHandle) ? InstagramHandle :
        !string.IsNullOrWhiteSpace(Email) ? Email : null;
}

public sealed record BookingGuestPetSnapshotView(
    string? DisplayName,
    Guid? AnimalTypeId,
    string? AnimalTypeCode,
    string? AnimalTypeName,
    Guid? BreedId,
    string? BreedCode,
    string? BreedName,
    Guid? CoatTypeId,
    string? CoatTypeCode,
    string? CoatTypeName,
    Guid? SizeCategoryId,
    string? SizeCategoryCode,
    string? SizeCategoryName,
    decimal? WeightKg,
    string? Notes);

public sealed record AppointmentPetView(Guid Id, Guid? ClientId, string AnimalTypeCode, string AnimalTypeName, string BreedName);
public sealed record AppointmentItemView(Guid Id, string ItemType, Guid OfferId, Guid OfferVersionId, string OfferCode, string OfferDisplayName, int Quantity, Guid PriceSnapshotId, Guid DurationSnapshotId, decimal PriceAmount, int ServiceMinutes, int ReservedMinutes);
public sealed record AppointmentListItemView(Guid Id, Guid? BookingRequestId, Guid PetId, Guid GroomerId, DateTime StartAtUtc, DateTime EndAtUtc, string Status, int VersionNo, int ItemCount, decimal TotalAmount);
public sealed record AppointmentDetailView(Guid Id, Guid? BookingRequestId, AppointmentPetView Pet, Guid GroomerId, DateTime StartAtUtc, DateTime EndAtUtc, string Status, int VersionNo, IReadOnlyCollection<AppointmentItemView> Items, decimal TotalAmount, int ServiceMinutes, int ReservedMinutes, string? CancellationReasonCode, string? CancellationNotes, DateTime? CancelledAtUtc, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
