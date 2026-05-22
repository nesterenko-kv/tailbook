using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Api.Public;

public sealed class CreatePublicBookingRequestRequest
{
    [FromClaim(TailbookClaimTypes.UserId, isRequired: false)]
    public Guid? UserId { get; set; }

    public PublicPetPayload Pet { get; set; } = new();
    public PublicRequesterPayload? Requester { get; set; }
    public Guid? PreferredGroomerId { get; set; }
    public string? SelectionMode { get; set; }
    public string? Notes { get; set; }
    public PublicPreferredTimePayload[] PreferredTimes { get; set; } = [];
    public PublicBookingItemPayload[] Items { get; set; } = [];
}