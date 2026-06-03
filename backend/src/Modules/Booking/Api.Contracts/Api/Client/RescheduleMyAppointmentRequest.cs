using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class RescheduleMyAppointmentRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }

    public Guid AppointmentId { get; set; }
    public Guid GroomerId { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public int ExpectedVersionNo { get; set; }
}
