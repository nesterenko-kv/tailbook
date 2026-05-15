using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.VisitOperations.Api.Admin.CheckInAppointment;

public sealed class CheckInAppointmentRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid ActorUserId { get; set; }

    public Guid AppointmentId { get; set; }
}
