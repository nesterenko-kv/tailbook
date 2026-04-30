using System.Text.Json;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Contracts;

namespace Tailbook.Modules.Booking.Infrastructure.Services;

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
    public async Task<ErrorOr<BookingRequestDetailView>> CreateBookingRequestAsync(CreateBookingRequestCommand command, string? actorUserId, CancellationToken cancellationToken)
    {
        PetQuoteProfile? pet = null;
        if (command.PetId.HasValue)
        {
            pet = await petQuoteProfileService.GetPetAsync(command.PetId.Value, cancellationToken);
            if (pet is null)
            {
                return Error.NotFound("Booking.PetNotFound", "Pet does not exist.");
            }
        }
        else if (command.GuestIntake is null)
        {
            return Error.Validation("Booking.PetRequired", "Booking request must reference a saved pet or include guest pet details.");
        }

        if (command.ClientId.HasValue)
        {
            var clientExists = await clientReferenceValidationService.ExistsAsync(command.ClientId.Value, cancellationToken);
            if (!clientExists)
            {
                return Error.NotFound("Booking.ClientNotFound", "Client does not exist.");
            }

            if (pet?.ClientId.HasValue == true && pet.ClientId.Value != command.ClientId.Value)
            {
                return Error.Validation("Booking.PetClientMismatch", "Selected pet does not belong to the specified client.");
            }
        }

        if (command.RequestedByContactId.HasValue)
        {
            var contactExists = await contactReferenceValidationService.ExistsAsync(command.RequestedByContactId.Value, cancellationToken);
            if (!contactExists)
            {
                return Error.NotFound("Booking.RequestedByContactNotFound", "Requested-by contact does not exist.");
            }
        }

        foreach (var item in command.Items)
        {
            var exists = await offerReferenceValidationService.ExistsAsync(item.OfferId, cancellationToken);
            if (!exists)
            {
                return Error.NotFound("Booking.OfferNotFound", "One or more requested offers do not exist.");
            }
        }

        var status = NormalizeStatus(command.InitialStatus, command.PetId.HasValue ? BookingRequestStatusCodes.Submitted : BookingRequestStatusCodes.NeedsReview);
        if (status.IsError)
        {
            return status.Errors;
        }

        string? selectionMode = null;
        if (!string.IsNullOrWhiteSpace(command.SelectionMode))
        {
            var selectionModeResult = NormalizeSelectionMode(command.SelectionMode);
            if (selectionModeResult.IsError)
            {
                return selectionModeResult.Errors;
            }

            selectionMode = selectionModeResult.Value;
        }

        var preferredTimeJson = SerializePreferredTimes(command.PreferredTimes);
        if (preferredTimeJson.IsError)
        {
            return preferredTimeJson.Errors;
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
            Status = status.Value,
            SelectionMode = selectionMode,
            GuestIntakeJson = SerializeGuestIntake(command.GuestIntake),
            PreferredTimeJson = preferredTimeJson.Value,
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

    public async Task<ErrorOr<BookingRequestDetailView>> AttachBookingRequestContextAsync(AttachBookingRequestContextCommand command, string? actorUserId, CancellationToken cancellationToken)
    {
        var bookingRequest = await dbContext.Set<BookingRequest>()
            .SingleOrDefaultAsync(x => x.Id == command.BookingRequestId, cancellationToken);

        if (bookingRequest is null)
        {
            return Error.NotFound("Booking.BookingRequestNotFound", "Booking request does not exist.");
        }

        if (bookingRequest.Status == BookingRequestStatusCodes.Converted)
        {
            return Error.Conflict("Booking.BookingRequestConverted", "Converted booking requests cannot be relinked.");
        }

        var pet = await petQuoteProfileService.GetPetAsync(command.PetId, cancellationToken);
        if (pet is null)
        {
            return Error.NotFound("Booking.PetNotFound", "Pet does not exist.");
        }

        if (command.ClientId.HasValue)
        {
            var clientExists = await clientReferenceValidationService.ExistsAsync(command.ClientId.Value, cancellationToken);
            if (!clientExists)
            {
                return Error.NotFound("Booking.ClientNotFound", "Client does not exist.");
            }

            if (pet.ClientId.HasValue && pet.ClientId.Value != command.ClientId.Value)
            {
                return Error.Validation("Booking.PetClientMismatch", "Selected pet does not belong to the specified client.");
            }
        }

        if (command.RequestedByContactId.HasValue)
        {
            var contactExists = await contactReferenceValidationService.ExistsAsync(command.RequestedByContactId.Value, cancellationToken);
            if (!contactExists)
            {
                return Error.NotFound("Booking.RequestedByContactNotFound", "Requested-by contact does not exist.");
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

        return (await GetBookingRequestAsync(bookingRequest.Id, cancellationToken))!;
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

    public async Task<ErrorOr<AppointmentDetailView>> ConvertBookingRequestToAppointmentAsync(ConvertBookingRequestToAppointmentCommand command, string? actorUserId, CancellationToken cancellationToken)
    {
        var bookingRequest = await dbContext.Set<BookingRequest>().SingleOrDefaultAsync(x => x.Id == command.BookingRequestId, cancellationToken);
        if (bookingRequest is null)
        {
            return Error.NotFound("Booking.BookingRequestNotFound", "Booking request does not exist.");
        }

        if (bookingRequest.Status == BookingRequestStatusCodes.Converted)
        {
            return Error.Conflict("Booking.BookingRequestAlreadyConverted", "Booking request has already been converted.");
        }

        if (!bookingRequest.PetId.HasValue)
        {
            return Error.Validation("Booking.GuestRequestRequiresSavedPet", "Guest booking requests must be linked to a saved pet before conversion to an appointment.");
        }

        var requestItems = await dbContext.Set<BookingRequestItem>()
            .Where(x => x.BookingRequestId == bookingRequest.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (requestItems.Count == 0)
        {
            return Error.Validation("Booking.BookingRequestItemsRequired", "Booking request must contain at least one requested item.");
        }

        var result = await CreateAppointmentInternalAsync(
            bookingRequest.Id,
            bookingRequest.PetId.Value,
            command.GroomerId,
            command.StartAtUtc,
            requestItems.Select(x => new PreviewQuoteItemCommand(x.OfferId, x.ItemType)).ToArray(),
            actorUserId,
            cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }

        bookingRequest.Status = BookingRequestStatusCodes.Converted;
        bookingRequest.UpdatedAtUtc = DateTime.UtcNow;
        await outboxPublisher.PublishAsync("booking", "BookingRequestConverted", new
        {
            bookingRequestId = bookingRequest.Id,
            appointmentId = result.Value.Id
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync("booking", "booking_request", bookingRequest.Id.ToString("D"), "CONVERT_TO_APPOINTMENT", ParseGuid(actorUserId), null, JsonSerializer.Serialize(new { appointmentId = result.Value.Id }), cancellationToken);

        return result.Value;
    }

    public Task<ErrorOr<AppointmentDetailView>> CreateAppointmentAsync(CreateAppointmentCommand command, string? actorUserId, CancellationToken cancellationToken)
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

    private async Task<ErrorOr<AppointmentDetailView>> CreateAppointmentInternalAsync(
        Guid? bookingRequestId,
        Guid petId,
        Guid groomerId,
        DateTime startAtUtc,
        IReadOnlyCollection<PreviewQuoteItemCommand> items,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var compositionResult = await bookingSnapshotComposer.ComposeAppointmentAsync(
            petId,
            groomerId,
            startAtUtc,
            items,
            actorUserId,
            cancellationToken);
        if (compositionResult.IsError)
        {
            return compositionResult.Errors;
        }

        var composition = compositionResult.Value;
        var appointmentPeriod = BookingTimeInputNormalizer.TryCreatePeriod(composition.StartAtUtc, composition.EndAtUtc);
        if (appointmentPeriod.IsError)
        {
            return appointmentPeriod.Errors;
        }

        var utcNow = DateTime.UtcNow;
        var actorGuid = ParseGuid(actorUserId);
        var appointment = Appointment.Create(
            Guid.NewGuid(),
            bookingRequestId,
            petId,
            groomerId,
            appointmentPeriod.Value,
            composition.Items.Select(x => new AppointmentItemDraft(
                x.OfferType,
                x.OfferId,
                x.OfferVersionId,
                x.OfferCode,
                x.DisplayName,
                1,
                x.PriceSnapshot.Id,
                x.DurationSnapshot.Id)).ToArray(),
            actorGuid,
            utcNow);

        dbContext.Set<Appointment>().Add(appointment);

        await outboxPublisher.PublishAsync("booking", "AppointmentCreated", new
        {
            appointmentId = appointment.Id,
            bookingRequestId = appointment.BookingRequestId,
            petId = appointment.PetId,
            groomerId = appointment.GroomerId,
            startAtUtc = appointment.StartAtUtc,
            endAtUtc = appointment.EndAtUtc,
            status = appointment.Status,
            versionNo = appointment.VersionNo
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

    public async Task<ErrorOr<AppointmentDetailView>> RescheduleAppointmentAsync(RescheduleAppointmentCommand command, string? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.Set<Appointment>().SingleOrDefaultAsync(x => x.Id == command.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Error.NotFound("Booking.AppointmentNotFound", "Appointment does not exist.");
        }

        var version = EnsureVersion(appointment, command.ExpectedVersionNo);
        if (version.IsError)
        {
            return version.Errors;
        }

        if (appointment.Status is AppointmentStatusCodes.Cancelled or AppointmentStatusCodes.Closed)
        {
            return Error.Conflict("Booking.AppointmentNotMutable", "Appointment is not mutable in its current status.");
        }

        var normalizedStartAtUtcResult = BookingTimeInputNormalizer.TryAssumeUtc(command.StartAtUtc, nameof(command.StartAtUtc));
        if (normalizedStartAtUtcResult.IsError)
        {
            return normalizedStartAtUtcResult.Errors;
        }

        var normalizedStartAtUtc = normalizedStartAtUtcResult.Value;

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

        var availabilityResult = await staffSchedulingService.CheckAvailabilityAsync(
            command.GroomerId,
            appointment.PetId,
            items.Select(x => x.OfferId).ToArray(),
            normalizedStartAtUtc,
            baseReservedMinutes,
            appointment.Id,
            cancellationToken);
        if (availabilityResult.IsError)
        {
            return availabilityResult.Errors;
        }

        var availability = availabilityResult.Value;
        if (!availability.IsAvailable)
        {
            return Error.Validation("Booking.AppointmentSlotUnavailable", string.Join(" ", availability.Reasons));
        }

        var appointmentPeriod = BookingTimeInputNormalizer.TryCreatePeriod(normalizedStartAtUtc, availability.EndAtUtc);
        if (appointmentPeriod.IsError)
        {
            return appointmentPeriod.Errors;
        }

        appointment.Reschedule(
            command.GroomerId,
            appointmentPeriod.Value,
            ParseGuid(actorUserId),
            DateTime.UtcNow);

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
        return (await GetAppointmentAsync(appointment.Id, cancellationToken))!;
    }

    public async Task<ErrorOr<AppointmentDetailView>> CancelAppointmentAsync(CancelAppointmentCommand command, string? actorUserId, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.Set<Appointment>().SingleOrDefaultAsync(x => x.Id == command.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Error.NotFound("Booking.AppointmentNotFound", "Appointment does not exist.");
        }

        var version = EnsureVersion(appointment, command.ExpectedVersionNo);
        if (version.IsError)
        {
            return version.Errors;
        }

        if (appointment.Status is AppointmentStatusCodes.Cancelled or AppointmentStatusCodes.Closed)
        {
            return Error.Conflict("Booking.AppointmentCancellationFailed", "Appointment is not mutable in its current status.");
        }

        if (string.IsNullOrWhiteSpace(command.ReasonCode))
        {
            return Error.Validation("Booking.CancellationReasonRequired", "Cancellation reason code is required.");
        }

        appointment.Cancel(command.ReasonCode, command.Notes, ParseGuid(actorUserId), DateTime.UtcNow);

        await outboxPublisher.PublishAsync("booking", "AppointmentCancelled", new
        {
            appointmentId = appointment.Id,
            status = appointment.Status,
            reasonCode = appointment.CancellationReasonCode,
            versionNo = appointment.VersionNo
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync("booking", "appointment", appointment.Id.ToString("D"), "CANCEL", ParseGuid(actorUserId), null, JsonSerializer.Serialize(new { appointment.CancellationReasonCode, appointment.VersionNo }), cancellationToken);
        return (await GetAppointmentAsync(appointment.Id, cancellationToken))!;
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

    private static ErrorOr<bool> EnsureVersion(Appointment appointment, int expectedVersionNo)
    {
        if (!appointment.HasVersion(expectedVersionNo))
        {
            return Error.Conflict("Booking.AppointmentVersionMismatch", $"Appointment version mismatch. Expected {expectedVersionNo}, actual {appointment.VersionNo}.");
        }

        return true;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ErrorOr<string> NormalizeStatus(string? status, string fallback)
    {
        var normalized = NormalizeOptional(status);
        return normalized switch
        {
            null => fallback,
            _ when BookingRequestStatusCodes.All.Contains(normalized, StringComparer.OrdinalIgnoreCase) =>
                BookingRequestStatusCodes.All.Single(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)),
            _ => Error.Validation("Booking.UnknownBookingRequestStatus", $"Unknown booking request status '{status}'.")
        };
    }

    private static ErrorOr<string> NormalizeSelectionMode(string selectionMode)
    {
        var normalized = NormalizeOptional(selectionMode);
        return normalized switch
        {
            _ when string.Equals(normalized, BookingRequestSelectionModeCodes.AnySuitableGroomer, StringComparison.OrdinalIgnoreCase) => BookingRequestSelectionModeCodes.AnySuitableGroomer,
            _ when string.Equals(normalized, BookingRequestSelectionModeCodes.SpecificGroomer, StringComparison.OrdinalIgnoreCase) => BookingRequestSelectionModeCodes.SpecificGroomer,
            _ when string.Equals(normalized, BookingRequestSelectionModeCodes.ExactSlot, StringComparison.OrdinalIgnoreCase) => BookingRequestSelectionModeCodes.ExactSlot,
            _ when string.Equals(normalized, BookingRequestSelectionModeCodes.PreferredWindow, StringComparison.OrdinalIgnoreCase) => BookingRequestSelectionModeCodes.PreferredWindow,
            _ => Error.Validation("Booking.UnknownSelectionMode", $"Unknown selection mode '{selectionMode}'.")
        };
    }

    private static ErrorOr<string?> SerializePreferredTimes(IReadOnlyCollection<PreferredTimeWindowCommand> windows)
    {
        if (windows.Count == 0)
        {
            return (string?)null;
        }

        var normalizedWindows = new List<PreferredTimeWindowView>();
        foreach (var window in windows)
        {
            var startAtUtc = BookingTimeInputNormalizer.TryAssumeUtc(window.StartAtUtc, nameof(window.StartAtUtc));
            if (startAtUtc.IsError)
            {
                return startAtUtc.Errors;
            }

            var endAtUtc = BookingTimeInputNormalizer.TryAssumeUtc(window.EndAtUtc, nameof(window.EndAtUtc));
            if (endAtUtc.IsError)
            {
                return endAtUtc.Errors;
            }

            if (endAtUtc.Value <= startAtUtc.Value)
            {
                return Error.Validation("Booking.InvalidPreferredTimeWindow", "Preferred time window end time must be after start time.");
            }

            normalizedWindows.Add(new PreferredTimeWindowView(startAtUtc.Value, endAtUtc.Value, NormalizeOptional(window.Label)));
        }

        return JsonSerializer.Serialize(normalizedWindows);
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
