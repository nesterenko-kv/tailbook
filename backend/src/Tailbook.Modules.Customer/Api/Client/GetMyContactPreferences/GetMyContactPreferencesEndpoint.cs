using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Abstractions.Security;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Customer.Api.Client.GetMyContactPreferences;

public sealed class GetMyContactPreferencesEndpoint(IClientPortalActorService actorService, IClientPortalCustomerQueries queries)
    : Endpoint<GetMyContactPreferencesRequest, ClientContactPreferencesView>
{
    public override void Configure()
    {
        Get("/api/client/me/contact-preferences");
        Description(x => x.WithTags("Client Portal CRM"));
        PermissionsAll(PermissionCodes.ClientContactPreferencesRead);
    }

    public override async Task HandleAsync(GetMyContactPreferencesRequest req, CancellationToken ct)
    {
        var actor = await actorService.GetActorAsync(req.UserId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var result = await queries.GetContactPreferencesAsync(actor.ContactPersonId, ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(result, cancellation: ct);
    }
}

public sealed class UpdateMyContactPreferencesEndpoint(IClientPortalActorService actorService, IClientPortalCustomerQueries queries)
    : Endpoint<UpdateMyContactPreferencesRequest, ClientContactPreferencesView>
{
    public override void Configure()
    {
        Patch("/api/client/me/contact-preferences");
        Description(x => x.WithTags("Client Portal CRM"));
        PermissionsAll(PermissionCodes.ClientContactPreferencesWrite);
    }

    public override async Task HandleAsync(UpdateMyContactPreferencesRequest req, CancellationToken ct)
    {
        var actor = await actorService.GetActorAsync(req.UserId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var result = await queries.UpdateContactPreferencesAsync(
            actor.ContactPersonId,
            new UpdateClientContactPreferencesCommand(req.Methods.Select(x => new UpdateClientContactMethodCommand(x.MethodType, x.Value, x.IsPreferred, x.Notes)).ToArray()),
            ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, cancellation: ct);
    }
}

public sealed class GetMyContactPreferencesRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }
}

public sealed class UpdateMyContactPreferencesRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }

    public UpdateMyContactMethodPayload[] Methods { get; set; } = [];
}

public sealed class UpdateMyContactMethodPayload
{
    public string MethodType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateMyContactPreferencesRequestValidator : Validator<UpdateMyContactPreferencesRequest>
{
    public UpdateMyContactPreferencesRequestValidator()
    {
        RuleFor(x => x.Methods).NotEmpty();
        RuleForEach(x => x.Methods).ChildRules(method =>
        {
            method.RuleFor(x => x.MethodType).NotEmpty().MaximumLength(32);
            method.RuleFor(x => x.Value).NotEmpty().MaximumLength(256);
            method.RuleFor(x => x.Notes).MaximumLength(500);
        });
    }
}
