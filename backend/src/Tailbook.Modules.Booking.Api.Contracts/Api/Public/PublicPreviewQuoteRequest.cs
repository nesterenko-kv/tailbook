using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Api.Public;

public sealed class PublicPreviewQuoteRequest
{
    [FromClaim(TailbookClaimTypes.UserId, isRequired: false)]
    public Guid? UserId { get; set; }

    public PublicPetPayload Pet { get; set; } = new();
    public PublicBookingItemPayload[] Items { get; set; } = [];
}