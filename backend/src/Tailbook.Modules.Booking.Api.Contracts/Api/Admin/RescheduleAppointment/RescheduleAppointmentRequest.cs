using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Api.Admin.RescheduleAppointment;

public sealed class RescheduleAppointmentRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid ActorUserId { get; set; }

    public Guid AppointmentId { get; set; }
    public Guid GroomerId { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public int ExpectedVersionNo { get; set; }
}
