using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class ListMyBookableOffersEndpoint(
    IClientPortalActorService actorService,
    ClientPortalBookingQueries queries
)
    : Endpoint<ListMyBookableOffersRequest, IReadOnlyCollection<ClientBookableOfferResponse>>
{
    public override void Configure()
    {
        Get("/api/client/booking-offers");
        Description(x => x.WithTags("Client Portal Booking"));
        Permissions("client.booking.write");
    }

    public override async Task HandleAsync(ListMyBookableOffersRequest req, CancellationToken ct)
    {
        var actor = await actorService.GetActorAsync(req.UserId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var petId = req.PetId;
        var result = await queries.ListMyBookableOffersAsync(actor.ClientId, petId, ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

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
}
