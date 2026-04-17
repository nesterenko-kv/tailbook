using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Booking.Application;
using Tailbook.Modules.Booking.Api.Client;
using Tailbook.Modules.Booking.Contracts;
using static Tailbook.Modules.Booking.Api.Public.PublicBookingEndpointMapper;

namespace Tailbook.Modules.Booking.Api.Public;

public sealed class ListPublicBookableOffersEndpoint(
    ICurrentUser currentUser,
    IClientPortalActorService actorService,
    PublicBookingQueries queries)
    : Endpoint<PublicBookableOffersRequest, IReadOnlyCollection<ClientBookableOfferResponse>>
{
    public override void Configure()
    {
        Post("/api/public/booking-offers");
        AllowAnonymous();
        Description(x => x.WithTags("Public Booking"));
    }

    public override async Task HandleAsync(PublicBookableOffersRequest req, CancellationToken ct)
    {
        try
        {
            var actor = await ResolveActorAsync(currentUser, actorService, ct);
            var result = await queries.ListBookableOffersAsync(actor, MapPet(req.Pet), ct);
            await Send.OkAsync(result.Select(x => new ClientBookableOfferResponse
            {
                Id = x.Id,
                OfferType = x.OfferType,
                DisplayName = x.DisplayName,
                Currency = x.Currency,
                PriceAmount = x.PriceAmount,
                ServiceMinutes = x.ServiceMinutes,
                ReservedMinutes = x.ReservedMinutes
            }).ToArray(), ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class PreviewPublicQuoteEndpoint(
    ICurrentUser currentUser,
    IClientPortalActorService actorService,
    PublicBookingQueries queries)
    : Endpoint<PublicPreviewQuoteRequest, PublicQuotePreviewResponse>
{
    public override void Configure()
    {
        Post("/api/public/quotes/preview");
        AllowAnonymous();
        Description(x => x.WithTags("Public Booking"));
    }

    public override async Task HandleAsync(PublicPreviewQuoteRequest req, CancellationToken ct)
    {
        try
        {
            var actor = await ResolveActorAsync(currentUser, actorService, ct);
            var result = await queries.PreviewQuoteAsync(
                actor,
                new PublicPreviewQuoteCommand(
                    MapPet(req.Pet),
                    req.Items.Select(x => new PreviewQuoteItemCommand(x.OfferId, x.ItemType)).ToArray()),
                ct);

            await Send.OkAsync(MapQuote(result), ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class BuildPublicBookingPlannerEndpoint(
    ICurrentUser currentUser,
    IClientPortalActorService actorService,
    PublicBookingQueries queries)
    : Endpoint<PublicBookingPlannerRequest, PublicBookingPlannerResponse>
{
    public override void Configure()
    {
        Post("/api/public/booking-planner");
        AllowAnonymous();
        Description(x => x.WithTags("Public Booking"));
    }

    public override async Task HandleAsync(PublicBookingPlannerRequest req, CancellationToken ct)
    {
        if (!DateOnly.TryParse(req.LocalDate, out var localDate))
        {
            AddError("localDate must be a valid date in YYYY-MM-DD format.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        try
        {
            var actor = await ResolveActorAsync(currentUser, actorService, ct);
            var result = await queries.BuildPlannerAsync(
                actor,
                new PublicBookingPlannerCommand(
                    MapPet(req.Pet),
                    localDate,
                    req.Items.Select(x => new PreviewQuoteItemCommand(x.OfferId, x.ItemType)).ToArray()),
                ct);

            await Send.OkAsync(new PublicBookingPlannerResponse
            {
                Quote = MapQuote(result.Quote),
                AnySuitableSlots = result.AnySuitableSlots.Select(MapSlot).ToArray(),
                Groomers = result.Groomers.Select(x => new PublicPlannerGroomerResponse
                {
                    GroomerId = x.GroomerId,
                    DisplayName = x.DisplayName,
                    CanTakeRequest = x.CanTakeRequest,
                    ReservedMinutes = x.ReservedMinutes,
                    Reasons = x.Reasons.ToArray(),
                    Slots = x.Slots.Select(MapSlot).ToArray()
                }).ToArray()
            }, ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class CreatePublicBookingRequestEndpoint(
    ICurrentUser currentUser,
    IClientPortalActorService actorService,
    PublicBookingQueries publicBookingQueries,
    BookingManagementQueries bookingManagementQueries)
    : Endpoint<CreatePublicBookingRequestRequest, BookingRequestDetailView>
{
    public override void Configure()
    {
        Post("/api/public/booking-requests");
        AllowAnonymous();
        Description(x => x.WithTags("Public Booking"));
    }

    public override async Task HandleAsync(CreatePublicBookingRequestRequest req, CancellationToken ct)
    {
        try
        {
            var actor = await ResolveActorAsync(currentUser, actorService, ct);
            if (actor is null && IsMissingActionableContact(req.Requester))
            {
                AddError("Provide your name and at least one contact method so the salon can follow up.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            var resolvedPet = await publicBookingQueries.ResolvePetAsync(actor, MapPet(req.Pet), ct);
            var result = await bookingManagementQueries.CreateBookingRequestAsync(
                new CreateBookingRequestCommand(
                    actor?.ClientId,
                    req.Pet.PetId,
                    actor?.ContactPersonId,
                    BookingChannelCodes.PublicWidget,
                    req.Notes,
                    req.PreferredTimes.Select(x => new PreferredTimeWindowCommand(x.StartAtUtc, x.EndAtUtc, x.Label)).ToArray(),
                    req.Items.Select(x => new CreateBookingRequestItemCommand(x.OfferId, x.ItemType, x.RequestedNotes)).ToArray(),
                    req.PreferredGroomerId,
                    req.SelectionMode,
                    BuildGuestIntake(req, resolvedPet),
                    req.Pet.PetId.HasValue ? BookingRequestStatusCodes.Submitted : BookingRequestStatusCodes.NeedsReview),
                currentUser.UserId,
                ct);

            await Send.ResponseAsync(result, StatusCodes.Status201Created, ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class PublicBookableOffersRequest
{
    public PublicPetPayload Pet { get; set; } = new();
}

public sealed class PublicPreviewQuoteRequest
{
    public PublicPetPayload Pet { get; set; } = new();
    public PublicBookingItemPayload[] Items { get; set; } = [];
}

public sealed class PublicBookingPlannerRequest
{
    public PublicPetPayload Pet { get; set; } = new();
    public string LocalDate { get; set; } = string.Empty;
    public PublicBookingItemPayload[] Items { get; set; } = [];
}

public sealed class CreatePublicBookingRequestRequest
{
    public PublicPetPayload Pet { get; set; } = new();
    public PublicRequesterPayload? Requester { get; set; }
    public Guid? PreferredGroomerId { get; set; }
    public string? SelectionMode { get; set; }
    public string? Notes { get; set; }
    public PublicPreferredTimePayload[] PreferredTimes { get; set; } = [];
    public PublicBookingItemPayload[] Items { get; set; } = [];
}

public sealed class PublicPetPayload
{
    public Guid? PetId { get; set; }
    public Guid? AnimalTypeId { get; set; }
    public Guid? BreedId { get; set; }
    public Guid? CoatTypeId { get; set; }
    public Guid? SizeCategoryId { get; set; }
    public decimal? WeightKg { get; set; }
    public string? PetName { get; set; }
    public string? Notes { get; set; }
}

public sealed class PublicRequesterPayload
{
    public string? DisplayName { get; set; }
    public string? Phone { get; set; }
    public string? InstagramHandle { get; set; }
    public string? Email { get; set; }
    public string? PreferredContactMethodCode { get; set; }
}

public sealed class PublicPreferredTimePayload
{
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public string? Label { get; set; }
}

public sealed class PublicBookingItemPayload
{
    public Guid OfferId { get; set; }
    public string? ItemType { get; set; }
    public string? RequestedNotes { get; set; }
}

public sealed class PublicQuotePreviewResponse
{
    public string Currency { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ServiceMinutes { get; set; }
    public int ReservedMinutes { get; set; }
    public QuoteItemPayload[] Items { get; set; } = [];
    public PriceLinePayload[] PriceLines { get; set; } = [];
    public DurationLinePayload[] DurationLines { get; set; } = [];

    public sealed class QuoteItemPayload
    {
        public Guid OfferId { get; set; }
        public string OfferType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal PriceAmount { get; set; }
        public int ServiceMinutes { get; set; }
        public int ReservedMinutes { get; set; }
    }

    public sealed class PriceLinePayload
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public sealed class DurationLinePayload
    {
        public string Label { get; set; } = string.Empty;
        public int Minutes { get; set; }
    }
}

public sealed class PublicBookingPlannerResponse
{
    public PublicQuotePreviewResponse Quote { get; set; } = new();
    public PublicPlannerSlotResponse[] AnySuitableSlots { get; set; } = [];
    public PublicPlannerGroomerResponse[] Groomers { get; set; } = [];
}

public sealed class PublicPlannerGroomerResponse
{
    public Guid GroomerId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool CanTakeRequest { get; set; }
    public int ReservedMinutes { get; set; }
    public string[] Reasons { get; set; } = [];
    public PublicPlannerSlotResponse[] Slots { get; set; } = [];
}

public sealed class PublicPlannerSlotResponse
{
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public Guid[] GroomerIds { get; set; } = [];
}

public sealed class PublicBookableOffersRequestValidator : Validator<PublicBookableOffersRequest>
{
    public PublicBookableOffersRequestValidator()
    {
        RuleFor(x => x.Pet).SetValidator(new PublicPetPayloadValidator());
    }
}

public sealed class PublicPreviewQuoteRequestValidator : Validator<PublicPreviewQuoteRequest>
{
    public PublicPreviewQuoteRequestValidator()
    {
        RuleFor(x => x.Pet).SetValidator(new PublicPetPayloadValidator());
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new PublicBookingItemPayloadValidator());
    }
}

public sealed class PublicBookingPlannerRequestValidator : Validator<PublicBookingPlannerRequest>
{
    public PublicBookingPlannerRequestValidator()
    {
        RuleFor(x => x.Pet).SetValidator(new PublicPetPayloadValidator());
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new PublicBookingItemPayloadValidator());
        RuleFor(x => x.LocalDate).NotEmpty();
    }
}

public sealed class CreatePublicBookingRequestRequestValidator : Validator<CreatePublicBookingRequestRequest>
{
    public CreatePublicBookingRequestRequestValidator()
    {
        RuleFor(x => x.Pet).SetValidator(new PublicPetPayloadValidator());
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new PublicBookingItemPayloadValidator());
        RuleForEach(x => x.PreferredTimes).SetValidator(new PublicPreferredTimePayloadValidator());
        RuleFor(x => x.SelectionMode).MaximumLength(32);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Requester).SetValidator(new PublicRequesterPayloadValidator()!).When(x => x.Requester is not null);
    }
}

public sealed class PublicPetPayloadValidator : AbstractValidator<PublicPetPayload>
{
    public PublicPetPayloadValidator()
    {
        RuleFor(x => x).Must(x => x.PetId.HasValue || (x.AnimalTypeId.HasValue && x.BreedId.HasValue))
            .WithMessage("Provide a saved petId or choose both animal type and breed.");
        RuleFor(x => x.PetName).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class PublicRequesterPayloadValidator : AbstractValidator<PublicRequesterPayload>
{
    public PublicRequesterPayloadValidator()
    {
        RuleFor(x => x.DisplayName).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(64);
        RuleFor(x => x.InstagramHandle).MaximumLength(120);
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.PreferredContactMethodCode).MaximumLength(32);
    }
}

public sealed class PublicPreferredTimePayloadValidator : AbstractValidator<PublicPreferredTimePayload>
{
    public PublicPreferredTimePayloadValidator()
    {
        RuleFor(x => x.StartAtUtc).NotEmpty();
        RuleFor(x => x.EndAtUtc).NotEmpty().GreaterThan(x => x.StartAtUtc);
        RuleFor(x => x.Label).MaximumLength(200);
    }
}

public sealed class PublicBookingItemPayloadValidator : AbstractValidator<PublicBookingItemPayload>
{
    public PublicBookingItemPayloadValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.ItemType).MaximumLength(32);
        RuleFor(x => x.RequestedNotes).MaximumLength(1000);
    }
}

internal static class PublicBookingEndpointMapper
{
    public static PublicPetSelectionCommand MapPet(PublicPetPayload payload)
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
            StartAtUtc = slot.StartAtUtc,
            EndAtUtc = slot.EndAtUtc,
            GroomerIds = slot.GroomerIds.ToArray()
        };

    public static GuestBookingIntakeCommand BuildGuestIntake(CreatePublicBookingRequestRequest req, PublicPetResolutionView resolvedPet)
        => new(
            req.Requester is null
                ? null
                : new GuestBookingRequesterCommand(
                    Normalize(req.Requester.DisplayName),
                    Normalize(req.Requester.Phone),
                    Normalize(req.Requester.InstagramHandle),
                    Normalize(req.Requester.Email),
                    Normalize(req.Requester.PreferredContactMethodCode)),
            new GuestBookingPetCommand(
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
        ICurrentUser currentUser,
        IClientPortalActorService actorService,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var userId))
        {
            return null;
        }

        return await actorService.GetActorAsync(userId, cancellationToken);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
