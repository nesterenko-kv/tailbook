using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.VisitOperations.Api.Admin.ApplyVisitAdjustment;

public sealed class ApplyVisitAdjustmentRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid ActorUserId { get; set; }

    public Guid VisitId { get; set; }
    public int Sign { get; set; }
    public decimal Amount { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid? TargetItemId { get; set; }
}
