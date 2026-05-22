using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.Api.Public;

internal static class PublicBookingEndpointMapper
{
    public static PublicPetSelectionQuery MapPet(PublicPetPayload payload)
        => new(
            payload.PetId,
            payload.AnimalTypeId,
            payload.BreedId,
            payload.CoatTypeId,
            payload.SizeCategoryId,
            payload.WeightKg,
            payload.PetName,
            payload.Notes);

    public static PublicQuotePreviewResponse MapQuote(QuotePreviewView quote)
        => new()
        {
            Currency = quote.PriceSnapshot.Currency,
            TotalAmount = quote.PriceSnapshot.TotalAmount,
            ServiceMinutes = quote.DurationSnapshot.ServiceMinutes,
            ReservedMinutes = quote.DurationSnapshot.ReservedMinutes,
            Items = quote.Items.Select(x => new PublicQuotePreviewResponse.QuoteItemPayload
            {
                OfferId = x.OfferId,
                OfferType = x.OfferType,
                DisplayName = x.DisplayName,
                PriceAmount = x.PriceAmount,
                ServiceMinutes = x.ServiceMinutes,
                ReservedMinutes = x.ReservedMinutes
            }).ToArray(),
            PriceLines = quote.PriceSnapshot.Lines.Select(x => new PublicQuotePreviewResponse.PriceLinePayload
            {
                Label = x.Label,
                Amount = x.Amount
            }).ToArray(),
            DurationLines = quote.DurationSnapshot.Lines.Select(x => new PublicQuotePreviewResponse.DurationLinePayload
            {
                Label = x.Label,
                Minutes = x.Minutes
            }).ToArray()
        };

    public static PublicPlannerSlotResponse MapSlot(PublicPlannerSlotView slot)
        => new()
        {
            StartAt = slot.StartAt,
            EndAt = slot.EndAt,
            GroomerIds = slot.GroomerIds.ToArray()
        };

    public static GuestBookingIntakeInput BuildGuestIntake(CreatePublicBookingRequestRequest req, PublicPetResolutionView resolvedPet)
        => new(
            req.Requester is null
                ? null
                : new GuestBookingRequesterInput(
                    Normalize(req.Requester.DisplayName),
                    Normalize(req.Requester.Phone),
                    Normalize(req.Requester.InstagramHandle),
                    Normalize(req.Requester.Email),
                    Normalize(req.Requester.PreferredContactMethodCode)),
            new GuestBookingPetInput(
                Normalize(req.Pet.PetName),
                resolvedPet.Pet.AnimalTypeId,
                resolvedPet.Pet.AnimalTypeCode,
                resolvedPet.Pet.AnimalTypeName,
                resolvedPet.Pet.BreedId,
                resolvedPet.Pet.BreedCode,
                resolvedPet.Pet.BreedName,
                resolvedPet.Pet.CoatTypeId,
                resolvedPet.Pet.CoatTypeCode,
                resolvedPet.Pet.CoatTypeName,
                resolvedPet.Pet.SizeCategoryId,
                resolvedPet.Pet.SizeCategoryCode,
                resolvedPet.Pet.SizeCategoryName,
                req.Pet.WeightKg,
                Normalize(req.Pet.Notes)));

    public static bool IsMissingActionableContact(PublicRequesterPayload? requester)
    {
        if (requester is null)
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(requester.DisplayName)
               || (string.IsNullOrWhiteSpace(requester.Phone)
                   && string.IsNullOrWhiteSpace(requester.InstagramHandle)
                   && string.IsNullOrWhiteSpace(requester.Email));
    }

    public static async Task<ClientPortalActor?> ResolveActorAsync(
        Guid? userId,
        IClientPortalActorService actorService,
        CancellationToken cancellationToken)
    {
        if (!userId.HasValue)
        {
            return null;
        }

        var actorResult = await actorService.GetActorAsync(userId.Value, cancellationToken);
        return actorResult.IsError ? null : actorResult.Value;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}