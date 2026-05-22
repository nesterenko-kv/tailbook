using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Api.Admin.ConvertBookingRequestToAppointment;

public sealed class ConvertBookingRequestToAppointmentRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid ActorUserId { get; set; }

    public Guid BookingRequestId { get; set; }
    public Guid GroomerId { get; set; }
    public DateTimeOffset StartAt { get; set; }
}
