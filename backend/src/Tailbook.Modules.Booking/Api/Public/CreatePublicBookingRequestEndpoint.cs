using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Public;

public sealed class CreatePublicBookingRequestEndpoint(
    IClientPortalActorService actorService,
    PublicBookingReadService publicBookingReadService)
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
        var actor = await PublicBookingEndpointMapper.ResolveActorAsync(req.UserId, actorService, ct);
        if (actor is null && PublicBookingEndpointMapper.IsMissingActionableContact(req.Requester))
        {
            AddError("Provide your name and at least one contact method so the salon can follow up.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var resolvedPet = await publicBookingReadService.ResolvePetAsync(actor, PublicBookingEndpointMapper.MapPet(req.Pet), ct);
        if (resolvedPet.IsError)
        {
            await Send.ResultAsync(resolvedPet.Errors.ToHttpResult());
            return;
        }

        var result = await new CreateBookingRequestUseCaseCommand(
                actor?.ClientId,
                req.Pet.PetId,
                actor?.ContactPersonId,
                BookingChannelCodes.PublicWidget,
                req.Notes,
                req.PreferredTimes.Select(x => new PreferredTimeWindowInput(x.StartAt, x.EndAt, x.Label)).ToArray(),
                req.Items.Select(x => new CreateBookingRequestItemInput(x.OfferId, x.ItemType, x.RequestedNotes)).ToArray(),
                req.UserId,
                req.PreferredGroomerId,
                req.SelectionMode,
                PublicBookingEndpointMapper.BuildGuestIntake(req, resolvedPet.Value),
                req.Pet.PetId.HasValue ? BookingRequestStatusCodes.Submitted : BookingRequestStatusCodes.NeedsReview)
            .ExecuteAsync(ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(result.Value, StatusCodes.Status201Created, ct);
    }
}
