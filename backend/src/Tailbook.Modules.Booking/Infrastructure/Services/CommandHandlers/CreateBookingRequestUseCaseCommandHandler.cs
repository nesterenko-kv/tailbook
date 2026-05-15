using System.Text.Json;
using ErrorOr;
using FastEndpoints;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Booking.Infrastructure.Services.CommandHandlers;

public sealed class CreateBookingRequestUseCaseCommandHandler(
    AppDbContext dbContext,
    IBookingManagementReadService bookingReadService,
    IPetQuoteProfileService petQuoteProfileService,
    IClientReferenceValidationService clientReferenceValidationService,
    IContactReferenceValidationService contactReferenceValidationService,
    IOfferReferenceValidationService offerReferenceValidationService,
    IAuditTrailService auditTrailService,
    IOutboxPublisher outboxPublisher,
    TimeProvider timeProvider)
    : ICommandHandler<CreateBookingRequestUseCaseCommand, ErrorOr<BookingRequestDetailView>>
{
    public async Task<ErrorOr<BookingRequestDetailView>> ExecuteAsync(CreateBookingRequestUseCaseCommand command, CancellationToken ct = default)
    {
        PetQuoteProfile? pet = null;
        if (command.PetId.HasValue)
        {
            pet = await petQuoteProfileService.GetPetAsync(command.PetId.Value, ct);
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
            var clientExists = await clientReferenceValidationService.ExistsAsync(command.ClientId.Value, ct);
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
            var contactExists = await contactReferenceValidationService.ExistsAsync(command.RequestedByContactId.Value, ct);
            if (!contactExists)
            {
                return Error.NotFound("Booking.RequestedByContactNotFound", "Requested-by contact does not exist.");
            }
        }

        foreach (var item in command.Items)
        {
            var exists = await offerReferenceValidationService.ExistsAsync(item.OfferId, ct);
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

        var utcNow = timeProvider.GetUtcNow();
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
            CreatedAt = utcNow,
            UpdatedAt = utcNow
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
            CreatedAt = utcNow
        }));

        await outboxPublisher.PublishAsync("booking", "BookingRequested", new
        {
            bookingRequestId = entity.Id,
            petId = entity.PetId,
            clientId = entity.ClientId,
            channel = entity.Channel,
            status = entity.Status,
            selectionMode = entity.SelectionMode
        }, ct);

        await dbContext.SaveChangesAsync(ct);
        await auditTrailService.RecordAsync(
            "booking",
            "booking_request",
            entity.Id.ToString("D"),
            "CREATE",
            command.ActorUserId,
            null,
            JsonSerializer.Serialize(new { entity.Status, entity.Channel, entity.SelectionMode, entity.PetId, entity.ClientId }),
            ct);

        return (await bookingReadService.GetBookingRequestAsync(entity.Id, ct))!;
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

    private static ErrorOr<string?> SerializePreferredTimes(IReadOnlyCollection<PreferredTimeWindowInput> windows)
    {
        if (windows.Count == 0)
        {
            return (string?)null;
        }

        var normalizedWindows = new List<PreferredTimeWindowView>();
        foreach (var window in windows)
        {
            var startAt = BookingTimeInputNormalizer.AssumeUtc(window.StartAt, nameof(window.StartAt));
            if (startAt.IsError)
            {
                return startAt.Errors;
            }

            var endAt = BookingTimeInputNormalizer.AssumeUtc(window.EndAt, nameof(window.EndAt));
            if (endAt.IsError)
            {
                return endAt.Errors;
            }

            if (endAt.Value <= startAt.Value)
            {
                return Error.Validation("Booking.InvalidPreferredTimeWindow", "Preferred time window end time must be after start time.");
            }

            normalizedWindows.Add(new PreferredTimeWindowView(startAt.Value, endAt.Value, NormalizeOptional(window.Label)));
        }

        return JsonSerializer.Serialize(normalizedWindows);
    }

    private static string? SerializeGuestIntake(GuestBookingIntakeInput? guestIntake)
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
}
