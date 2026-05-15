using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Api.Public;

public sealed class PublicBookableOffersRequest
{
    [FromClaim(TailbookClaimTypes.UserId, isRequired: false)]
    public Guid? UserId { get; set; }

    public PublicPetPayload Pet { get; set; } = new();
}