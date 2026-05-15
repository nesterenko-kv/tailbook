using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Public;

public sealed class BuildPublicBookingPlannerEndpoint(
    IClientPortalActorService actorService,
    PublicBookingReadService queries)
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

        var actor = await PublicBookingEndpointMapper.ResolveActorAsync(req.UserId, actorService, ct);
        var result = await queries.BuildPlannerAsync(
            actor,
            new PublicBookingPlannerQuery(
                PublicBookingEndpointMapper.MapPet(req.Pet),
                localDate,
                req.Items.Select(x => new PreviewQuoteItemQuery(x.OfferId, x.ItemType)).ToArray()),
            ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(new PublicBookingPlannerResponse
        {
            Quote = PublicBookingEndpointMapper.MapQuote(result.Value.Quote),
            AnySuitableSlots = result.Value.AnySuitableSlots.Select(PublicBookingEndpointMapper.MapSlot).ToArray(),
            Groomers = result.Value.Groomers.Select(x => new PublicPlannerGroomerResponse
            {
                GroomerId = x.GroomerId,
                DisplayName = x.DisplayName,
                CanTakeRequest = x.CanTakeRequest,
                ReservedMinutes = x.ReservedMinutes,
                Reasons = x.Reasons.ToArray(),
                Slots = x.Slots.Select(PublicBookingEndpointMapper.MapSlot).ToArray()
            }).ToArray()
        }, ct);
    }
}