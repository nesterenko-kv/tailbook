using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Customer.Application;
using Tailbook.Modules.Identity.Contracts;

namespace Tailbook.Modules.Customer.Api.Client.GetMyContactPreferences;

public sealed class GetMyContactPreferencesEndpoint(ICurrentUser currentUser, IClientPortalActorService actorService, ClientPortalCustomerQueries queries)
    : EndpointWithoutRequest<ClientContactPreferencesView>
{
    public override void Configure()
    {
        Get("/api/client/me/contact-preferences");
        Description(x => x.WithTags("Client Portal CRM"));
        PermissionsAll(PermissionCodes.ClientContactPreferencesRead);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var actor = await actorService.GetActorAsync(userId, ct);
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

public sealed class UpdateMyContactPreferencesEndpoint(ICurrentUser currentUser, IClientPortalActorService actorService, ClientPortalCustomerQueries queries)
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
        if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var actor = await actorService.GetActorAsync(userId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        try
        {
            var result = await queries.UpdateContactPreferencesAsync(
                actor.ContactPersonId,
                new UpdateClientContactPreferencesCommand(req.Methods.Select(x => new UpdateClientContactMethodCommand(x.MethodType, x.Value, x.IsPreferred, x.Notes)).ToArray()),
                ct);

            if (result is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.OkAsync(result, cancellation: ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class UpdateMyContactPreferencesRequest
{
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
        RuleForEach(x => x.Methods).SetValidator(new UpdateMyContactMethodPayloadValidator());
    }
}

public sealed class UpdateMyContactMethodPayloadValidator : AbstractValidator<UpdateMyContactMethodPayload>
{
    public UpdateMyContactMethodPayloadValidator()
    {
        RuleFor(x => x.MethodType).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
