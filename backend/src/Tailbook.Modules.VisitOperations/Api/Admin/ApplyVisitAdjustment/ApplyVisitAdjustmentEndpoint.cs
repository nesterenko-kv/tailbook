using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.VisitOperations.Application;

namespace Tailbook.Modules.VisitOperations.Api.Admin.ApplyVisitAdjustment;

public sealed class ApplyVisitAdjustmentEndpoint(ICurrentUser currentUser, VisitQueries visitQueries)
    : Endpoint<ApplyVisitAdjustmentRequest, VisitDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/visits/{visitId:guid}/adjustments");
        Description(x => x.WithTags("Admin Visits"));
        Permissions("visit.write");
    }

    public override async Task HandleAsync(ApplyVisitAdjustmentRequest req, CancellationToken ct)
    {
        var actorUserId = Guid.TryParse(currentUser.UserId, out var parsed) ? parsed : (Guid?)null;

        try
        {
            var result = await visitQueries.ApplyPriceAdjustmentAsync(req.VisitId, req.Sign, req.Amount, req.ReasonCode, req.Note, actorUserId, ct);
            if (result is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class ApplyVisitAdjustmentRequest
{
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
