using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Api.Public;

public sealed class PublicBookingPlannerRequest
{
    [FromClaim(TailbookClaimTypes.UserId, isRequired: false)]
    public Guid? UserId { get; set; }

    public PublicPetPayload Pet { get; set; } = new();
    public string LocalDate { get; set; } = string.Empty;
    public PublicBookingItemPayload[] Items { get; set; } = [];
}