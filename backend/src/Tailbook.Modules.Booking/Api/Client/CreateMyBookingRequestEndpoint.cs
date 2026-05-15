using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class CreateMyBookingRequestEndpoint(
    IClientPortalActorService actorService)
    : Endpoint<CreateMyBookingRequestRequest, BookingRequestDetailView>
{
    public override void Configure()
    {
        Post("/api/client/booking-requests");
        Description(x => x.WithTags("Client Portal Booking"));
        PermissionsAll("client.booking.write");
    }

    public override async Task HandleAsync(CreateMyBookingRequestRequest req, CancellationToken ct)
    {
        var actorResult = await actorService.GetActorAsync(req.UserId, ct);
        if (actorResult.IsError)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var actor = actorResult.Value;
        var result = await new CreateClientBookingRequestUseCaseCommand(
            actor,
            req.PetId,
            req.Notes,
            req.PreferredTimes.Select(x => new PreferredTimeWindowInput(x.StartAt, x.EndAt, x.Label))
                .ToArray(),
            req.Items.Select(x =>
                new CreateClientBookingRequestItemInput(x.OfferId, x.ItemType, x.RequestedNotes)).ToArray())
            .ExecuteAsync(ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(result.Value, StatusCodes.Status201Created, ct);
    }
}
