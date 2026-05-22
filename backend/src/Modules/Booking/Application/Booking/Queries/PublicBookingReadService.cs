using ErrorOr;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public sealed class PublicBookingReadService(
    IPetQuoteProfileService petQuoteProfileService,
    ICatalogOfferReadService catalogOfferReadService,
    ICatalogQuoteResolver catalogQuoteResolver,
    IStaffSchedulingService staffSchedulingService,
    IGroomerProfileReadService groomerProfileReadService,
    IBookingManagementReadService bookingManagementReadService,
    TimeProvider timeProvider)
{
    private const int SlotStepMinutes = 30;
    private const int MinimumLeadTimeMinutes = 30;

    public async Task<ErrorOr<PublicPetResolutionView>> ResolvePetAsync(
        ClientPortalActor? actor,
        PublicPetSelectionQuery command,
        CancellationToken cancellationToken)
    {
        if (command.PetId.HasValue)
        {
            if (actor is null)
            {
                return Error.Validation("Booking.ClientSessionRequired", "Saved pet selection requires an authenticated client session.");
            }

            var savedPet = await petQuoteProfileService.GetPetAsync(command.PetId.Value, cancellationToken);
            if (savedPet is null)
            {
                return Error.NotFound("Booking.PetNotFound", "Selected pet does not exist.");
            }

            if (!savedPet.ClientId.HasValue || savedPet.ClientId.Value != actor.ClientId)
            {
                return Error.Validation("Booking.PetClientMismatch", "Selected pet does not belong to the authenticated client.");
            }

            return new PublicPetResolutionView(savedPet, true);
        }

        if (!command.AnimalTypeId.HasValue || !command.BreedId.HasValue)
        {
            return Error.Validation("Booking.GuestPetTaxonomyRequired", "Animal type and breed are required when booking without a saved pet.");
        }

        var guestPet = await petQuoteProfileService.CreateAdHocAsync(
            new PetQuoteProfileInput(
                command.AnimalTypeId.Value,
                command.BreedId.Value,
                command.CoatTypeId,
                command.SizeCategoryId),
            cancellationToken);
        if (guestPet.IsError)
        {
            return guestPet.Errors;
        }

        return new PublicPetResolutionView(guestPet.Value, false);
    }

    public async Task<ErrorOr<IReadOnlyCollection<ClientBookableOfferView>>> ListBookableOffersAsync(
        ClientPortalActor? actor,
        PublicPetSelectionQuery pet,
        CancellationToken cancellationToken)
    {
        var resolvedPet = await ResolvePetAsync(actor, pet, cancellationToken);
        if (resolvedPet.IsError)
        {
            return resolvedPet.Errors;
        }

        var offers = (await catalogOfferReadService.ListActiveOffersAsync(cancellationToken))
            .Where(x => !string.Equals(x.OfferType, "AddOn", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (offers.Length == 0)
        {
            return Array.Empty<ClientBookableOfferView>();
        }

        var result = new List<ClientBookableOfferView>();
        foreach (var offer in offers)
        {
            var resolutionResult = await catalogQuoteResolver.ResolveAsync(
                resolvedPet.Value.Pet,
                [new QuotePreviewCatalogItem(offer.Id, offer.OfferType)],
                cancellationToken);
            if (resolutionResult.IsError)
            {
                continue;
            }

            var resolution = resolutionResult.Value;
            var item = resolution.Items.Single();
            result.Add(new ClientBookableOfferView(
                item.OfferId,
                item.OfferType,
                item.DisplayName,
                resolution.Currency,
                item.PriceAmount,
                item.ServiceMinutes,
                item.ReservedMinutes));
        }

        return result
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.OfferType)
            .ToArray();
    }

    public async Task<ErrorOr<QuotePreviewView>> PreviewQuoteAsync(
        ClientPortalActor? actor,
        PublicPreviewQuoteQuery command,
        CancellationToken cancellationToken)
    {
        var resolvedPet = await ResolvePetAsync(actor, command.Pet, cancellationToken);
        if (resolvedPet.IsError)
        {
            return resolvedPet.Errors;
        }

        return await CreateQuotePreviewAsync(resolvedPet.Value.Pet, command.Items, cancellationToken);
    }

    public async Task<ErrorOr<PublicBookingPlannerView>> BuildPlannerAsync(
        ClientPortalActor? actor,
        PublicBookingPlannerQuery command,
        CancellationToken cancellationToken)
    {
        var resolvedPet = await ResolvePetAsync(actor, command.Pet, cancellationToken);
        if (resolvedPet.IsError)
        {
            return resolvedPet.Errors;
        }

        var quote = await CreateQuotePreviewAsync(resolvedPet.Value.Pet, command.Items, cancellationToken);
        if (quote.IsError)
        {
            return quote.Errors;
        }

        var offerIds = command.Items.Select(x => x.OfferId).Distinct().ToArray();
        var groomers = await groomerProfileReadService.ListActiveAsync(cancellationToken);
        var earliestStartAt = timeProvider.GetUtcNow().AddMinutes(MinimumLeadTimeMinutes);

        var groomerViews = new List<PublicPlannerGroomerView>();
        foreach (var groomer in groomers)
        {
            var slotResult = await staffSchedulingService.GetAvailableSlotsAsync(
                groomer.GroomerId,
                resolvedPet.Value.Pet,
                offerIds,
                command.LocalDate,
                quote.Value.DurationSnapshot.ReservedMinutes,
                earliestStartAt,
                SlotStepMinutes,
                null,
                cancellationToken);
            if (slotResult.IsError)
            {
                groomerViews.Add(new PublicPlannerGroomerView(
                    groomer.GroomerId,
                    groomer.DisplayName,
                    false,
                    quote.Value.DurationSnapshot.ReservedMinutes,
                    slotResult.Errors.Select(error => error.Description).ToArray(),
                    []));
                continue;
            }

            var availability = slotResult.Value;
            var duration = availability.Duration;
            var slots = availability.Slots
                .Select(slot => new PublicPlannerSlotView(slot.StartAt, slot.EndAt, [groomer.GroomerId]))
                .ToArray();

            groomerViews.Add(new PublicPlannerGroomerView(
                groomer.GroomerId,
                groomer.DisplayName,
                true,
                duration.EffectiveReservedMinutes,
                slots.Length > 0 ? duration.Reasons : duration.Reasons.Concat(["No exact slots available on the selected date."]).ToArray(),
                slots));
        }

        var anySuitableSlots = groomerViews
            .Where(x => x.CanTakeRequest)
            .SelectMany(x => x.Slots)
            .GroupBy(x => new { x.StartAt, x.EndAt })
            .Select(x => new PublicPlannerSlotView(
                x.Key.StartAt,
                x.Key.EndAt,
                x.SelectMany(y => y.GroomerIds).Distinct().OrderBy(y => y).ToArray()))
            .OrderBy(x => x.StartAt)
            .ThenBy(x => x.EndAt)
            .ToArray();

        return new PublicBookingPlannerView(quote.Value, anySuitableSlots, groomerViews);
    }

    private async Task<ErrorOr<QuotePreviewView>> CreateQuotePreviewAsync(
        PetQuoteProfile pet,
        IReadOnlyCollection<PreviewQuoteItemQuery> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return Error.Validation("Booking.OfferRequired", "At least one offer must be selected.");
        }

        var resolutionResult = await catalogQuoteResolver.ResolveAsync(
            pet,
            items.Select(x => new QuotePreviewCatalogItem(x.OfferId, x.ItemType)).ToArray(),
            cancellationToken);
        if (resolutionResult.IsError)
        {
            return resolutionResult.Errors;
        }

        var resolution = resolutionResult.Value;
        return new QuotePreviewView(
            new PriceSnapshotView(
                Guid.Empty,
                resolution.Currency,
                resolution.TotalAmount,
                resolution.PriceLines
                    .OrderBy(x => x.SequenceNo)
                    .Select(x => new PriceSnapshotLineView(x.LineType, x.Label, x.Amount, x.SourceRuleId, x.SequenceNo))
                    .ToArray()),
            new DurationSnapshotView(
                Guid.Empty,
                resolution.ServiceMinutes,
                resolution.ReservedMinutes,
                resolution.DurationLines
                    .OrderBy(x => x.SequenceNo)
                    .Select(x => new DurationSnapshotLineView(x.LineType, x.Label, x.Minutes, x.SourceRuleId, x.SequenceNo))
                    .ToArray()),
            resolution.Items.Select(x => new QuotePreviewItemView(
                x.OfferId,
                x.OfferVersionId,
                x.OfferCode,
                x.OfferType,
                x.DisplayName,
                x.PriceAmount,
                x.ServiceMinutes,
                x.ReservedMinutes)).ToArray());
    }

    public async Task<BookingRequestDetailView?> GetBookingRequestStatusAsync(
        Guid bookingRequestId,
        CancellationToken cancellationToken)
    {
        return await bookingManagementReadService.GetBookingRequestAsync(bookingRequestId, cancellationToken);
    }
}
