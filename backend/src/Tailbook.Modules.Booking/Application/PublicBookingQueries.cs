using ErrorOr;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.Application;

public sealed class PublicBookingQueries(
    IPetQuoteProfileService petQuoteProfileService,
    ICatalogOfferReadService catalogOfferReadService,
    ICatalogQuoteResolver catalogQuoteResolver,
    IStaffSchedulingService staffSchedulingService,
    IGroomerProfileReadService groomerProfileReadService)
{
    private const int SlotStepMinutes = 30;
    private const int MinimumLeadTimeMinutes = 30;

    public async Task<ErrorOr<PublicPetResolutionView>> ResolvePetAsync(
        ClientPortalActor? actor,
        PublicPetSelectionCommand command,
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

        return new PublicPetResolutionView(guestPet, false);
    }

    public async Task<ErrorOr<IReadOnlyCollection<ClientBookableOfferView>>> ListBookableOffersAsync(
        ClientPortalActor? actor,
        PublicPetSelectionCommand pet,
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
            try
            {
                var resolution = await catalogQuoteResolver.ResolveAsync(
                    resolvedPet.Value.Pet,
                    [new QuotePreviewCatalogItem(offer.Id, offer.OfferType)],
                    cancellationToken);

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
            catch (InvalidOperationException)
            {
                // Skip offers that are not currently bookable for the selected pet profile.
            }
        }

        return result
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.OfferType)
            .ToArray();
    }

    public async Task<ErrorOr<QuotePreviewView>> PreviewQuoteAsync(
        ClientPortalActor? actor,
        PublicPreviewQuoteCommand command,
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
        PublicBookingPlannerCommand command,
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
        var earliestStartAtUtc = DateTime.UtcNow.AddMinutes(MinimumLeadTimeMinutes);

        var groomerViews = new List<PublicPlannerGroomerView>();
        foreach (var groomer in groomers)
        {
            try
            {
                var durationResult = await staffSchedulingService.ResolveReservedDurationAsync(
                    groomer.GroomerId,
                    resolvedPet.Value.Pet,
                    offerIds,
                    quote.Value.DurationSnapshot.ReservedMinutes,
                    cancellationToken);
                if (durationResult.IsError)
                {
                    groomerViews.Add(new PublicPlannerGroomerView(
                        groomer.GroomerId,
                        groomer.DisplayName,
                        false,
                        quote.Value.DurationSnapshot.ReservedMinutes,
                        durationResult.Errors.Select(error => error.Description).ToArray(),
                        []));
                    continue;
                }

                var duration = durationResult.Value;

                var windows = await staffSchedulingService.GetAvailabilityWindowsAsync(
                    groomer.GroomerId,
                    command.LocalDate,
                    cancellationToken);

                var slots = new List<PublicPlannerSlotView>();
                foreach (var window in windows)
                {
                    foreach (var slotStartAtUtc in GenerateSlotStarts(window, duration.EffectiveReservedMinutes, earliestStartAtUtc))
                    {
                        var availabilityResult = await staffSchedulingService.CheckAvailabilityAsync(
                            groomer.GroomerId,
                            resolvedPet.Value.Pet,
                            offerIds,
                            slotStartAtUtc,
                            quote.Value.DurationSnapshot.ReservedMinutes,
                            null,
                            cancellationToken);
                        if (availabilityResult.IsError)
                        {
                            continue;
                        }

                        var availability = availabilityResult.Value;
                        if (availability.IsAvailable)
                        {
                            slots.Add(new PublicPlannerSlotView(
                                slotStartAtUtc,
                                availability.EndAtUtc,
                                [groomer.GroomerId]));
                        }
                    }
                }

                groomerViews.Add(new PublicPlannerGroomerView(
                    groomer.GroomerId,
                    groomer.DisplayName,
                    true,
                    duration.EffectiveReservedMinutes,
                    slots.Count > 0 ? duration.Reasons : duration.Reasons.Concat(["No exact slots available on the selected date."]).ToArray(),
                    slots));
            }
            catch (InvalidOperationException ex)
            {
                groomerViews.Add(new PublicPlannerGroomerView(
                    groomer.GroomerId,
                    groomer.DisplayName,
                    false,
                    quote.Value.DurationSnapshot.ReservedMinutes,
                    [ex.Message],
                    []));
            }
        }

        var anySuitableSlots = groomerViews
            .Where(x => x.CanTakeRequest)
            .SelectMany(x => x.Slots)
            .GroupBy(x => new { x.StartAtUtc, x.EndAtUtc })
            .Select(x => new PublicPlannerSlotView(
                x.Key.StartAtUtc,
                x.Key.EndAtUtc,
                x.SelectMany(y => y.GroomerIds).Distinct().OrderBy(y => y).ToArray()))
            .OrderBy(x => x.StartAtUtc)
            .ThenBy(x => x.EndAtUtc)
            .ToArray();

        return new PublicBookingPlannerView(quote.Value, anySuitableSlots, groomerViews);
    }

    private async Task<ErrorOr<QuotePreviewView>> CreateQuotePreviewAsync(
        PetQuoteProfile pet,
        IReadOnlyCollection<PreviewQuoteItemCommand> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return Error.Validation("Booking.OfferRequired", "At least one offer must be selected.");
        }

        CatalogQuoteResolution resolution;
        try
        {
            resolution = await catalogQuoteResolver.ResolveAsync(
                pet,
                items.Select(x => new QuotePreviewCatalogItem(x.OfferId, x.ItemType)).ToArray(),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation("Booking.QuotePreviewFailed", ex.Message);
        }

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

    private static IReadOnlyCollection<DateTime> GenerateSlotStarts(
        AvailabilityWindowReadModel window,
        int reservedMinutes,
        DateTime earliestStartAtUtc)
    {
        var starts = new List<DateTime>();
        var latestStartAtUtc = window.EndAtUtc.AddMinutes(-reservedMinutes);
        if (latestStartAtUtc < window.StartAtUtc)
        {
            return starts;
        }

        var cursor = window.StartAtUtc > earliestStartAtUtc ? window.StartAtUtc : earliestStartAtUtc;
        while (cursor <= latestStartAtUtc)
        {
            starts.Add(cursor);
            cursor = cursor.AddMinutes(SlotStepMinutes);
        }

        return starts;
    }
}

public sealed record PublicPetSelectionCommand(
    Guid? PetId,
    Guid? AnimalTypeId,
    Guid? BreedId,
    Guid? CoatTypeId,
    Guid? SizeCategoryId,
    decimal? WeightKg,
    string? PetName,
    string? Notes);

public sealed record PublicPetResolutionView(
    PetQuoteProfile Pet,
    bool UsesSavedPet);

public sealed record PublicPreviewQuoteCommand(
    PublicPetSelectionCommand Pet,
    IReadOnlyCollection<PreviewQuoteItemCommand> Items);

public sealed record PublicBookingPlannerCommand(
    PublicPetSelectionCommand Pet,
    DateOnly LocalDate,
    IReadOnlyCollection<PreviewQuoteItemCommand> Items);

public sealed record PublicBookingPlannerView(
    QuotePreviewView Quote,
    IReadOnlyCollection<PublicPlannerSlotView> AnySuitableSlots,
    IReadOnlyCollection<PublicPlannerGroomerView> Groomers);

public sealed record PublicPlannerGroomerView(
    Guid GroomerId,
    string DisplayName,
    bool CanTakeRequest,
    int ReservedMinutes,
    IReadOnlyCollection<string> Reasons,
    IReadOnlyCollection<PublicPlannerSlotView> Slots);

public sealed record PublicPlannerSlotView(
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    IReadOnlyCollection<Guid> GroomerIds);
