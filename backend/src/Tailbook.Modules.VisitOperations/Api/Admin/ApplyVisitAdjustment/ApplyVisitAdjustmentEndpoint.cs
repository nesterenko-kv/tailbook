using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions.Security;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Admin.ApplyVisitAdjustment;

public sealed class ApplyVisitAdjustmentEndpoint(IVisitQueries visitQueries)
    : Endpoint<ApplyVisitAdjustmentRequest, VisitDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/visits/{visitId:guid}/adjustments");
        Description(x => x.WithTags("Admin Visits"));
        PermissionsAll(PermissionCodes.VisitAdjustmentsWrite);
    }

    public override async Task HandleAsync(ApplyVisitAdjustmentRequest req, CancellationToken ct)
    {
        var result = await visitQueries.ApplyPriceAdjustmentAsync(req.VisitId, req.Sign, req.Amount, req.ReasonCode, req.Note, req.ActorUserId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class ApplyVisitAdjustmentRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid VisitId { get; set; }
    public int Sign { get; set; }
    public decimal Amount { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public sealed class ApplyVisitAdjustmentRequestValidator : Validator<ApplyVisitAdjustmentRequest>
{
    public ApplyVisitAdjustmentRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.Sign).Must(x => x is -1 or 1);
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.ReasonCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
