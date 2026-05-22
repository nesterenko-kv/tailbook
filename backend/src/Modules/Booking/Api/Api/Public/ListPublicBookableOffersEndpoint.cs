using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Booking.Api.Client;
using static Tailbook.Modules.Booking.Api.Public.PublicBookingEndpointMapper;

namespace Tailbook.Modules.Booking.Api.Public;

public sealed class ListPublicBookableOffersEndpoint(
    IClientPortalActorService actorService,
    PublicBookingReadService queries)
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
        var actor = await ResolveActorAsync(req.UserId, actorService, ct);
        var result = await queries.ListBookableOffersAsync(actor, MapPet(req.Pet), ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value.Select(x => new ClientBookableOfferResponse
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
}