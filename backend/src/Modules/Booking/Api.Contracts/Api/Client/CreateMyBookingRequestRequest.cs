using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class CreateMyBookingRequestRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }

    public Guid PetId { get; set; }
    public string? Notes { get; set; }
    public ClientPreferredTimeWindowPayload[] PreferredTimes { get; set; } = [];
    public ClientBookingRequestItemPayload[] Items { get; set; } = [];
}