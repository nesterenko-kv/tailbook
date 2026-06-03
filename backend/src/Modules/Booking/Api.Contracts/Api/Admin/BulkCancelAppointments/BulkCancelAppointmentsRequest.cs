using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Api.Admin.BulkCancelAppointments;

public sealed class BulkCancelAppointmentsRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid ActorUserId { get; set; }

    public Guid[] AppointmentIds { get; set; } = [];
    public string ReasonCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
