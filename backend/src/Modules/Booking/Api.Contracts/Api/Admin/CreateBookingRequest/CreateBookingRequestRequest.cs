using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Api.Admin.CreateBookingRequest;

public sealed class CreateBookingRequestRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid ActorUserId { get; set; }

    public Guid? ClientId { get; set; }
    public Guid PetId { get; set; }
    public Guid? RequestedByContactId { get; set; }
    public string? Channel { get; set; }
    public string? Notes { get; set; }
    public PreferredTimeWindowPayload[] PreferredTimes { get; set; } = [];
    public BookingRequestItemPayload[] Items { get; set; } = [];
}
