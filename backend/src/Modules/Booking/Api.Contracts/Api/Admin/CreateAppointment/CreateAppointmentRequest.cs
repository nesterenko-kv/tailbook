using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Api.Admin.CreateAppointment;

public sealed class CreateAppointmentRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid ActorUserId { get; set; }

    public Guid PetId { get; set; }
    public Guid GroomerId { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public CreateAppointmentItemPayload[] Items { get; set; } = [];
}
