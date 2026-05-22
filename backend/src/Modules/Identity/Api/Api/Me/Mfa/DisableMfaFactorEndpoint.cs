using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Identity.Api.Me.Mfa;

public sealed class DisableMfaFactorEndpoint(IMfaFactorService mfaFactorService)
    : Endpoint<DisableMfaFactorRequest>
{
    public override void Configure()
    {
        Delete("/api/identity/me/mfa/factors/{factorId:guid}");
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(DisableMfaFactorRequest req, CancellationToken ct)
    {
        var result = await mfaFactorService.DisableFactorAsync(req.UserId, req.FactorId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

public sealed class DisableMfaFactorRequestValidator : Validator<DisableMfaFactorRequest>
{
    public DisableMfaFactorRequestValidator()
    {
        RuleFor(x => x.FactorId).NotEmpty();
    }
}
