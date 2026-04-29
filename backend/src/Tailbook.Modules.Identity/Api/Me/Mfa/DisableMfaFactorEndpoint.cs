using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Identity.Application;

namespace Tailbook.Modules.Identity.Api.Me.Mfa;

public sealed class DisableMfaFactorEndpoint(MfaFactorService mfaFactorService)
    : Endpoint<DisableMfaFactorRequest>
{
    public override void Configure()
    {
        Delete("/api/identity/me/mfa/factors/{factorId:guid}");
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(DisableMfaFactorRequest req, CancellationToken ct)
    {
        if (req.UserId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var disabled = await mfaFactorService.DisableFactorAsync(req.UserId.Value, req.FactorId, ct);
        if (!disabled)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

public sealed class DisableMfaFactorRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? UserId { get; set; }

    public Guid FactorId { get; set; }
}

public sealed class DisableMfaFactorRequestValidator : Validator<DisableMfaFactorRequest>
{
    public DisableMfaFactorRequestValidator()
    {
        RuleFor(x => x.FactorId).NotEmpty();
    }
}
