using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Customer.Api.Admin.LinkContactToPet;

public sealed class LinkContactToPetEndpoint : Endpoint<LinkContactToPetRequest, LinkContactToPetResponse>
{
    public override void Configure()
    {
        Post("/api/admin/pets/{petId:guid}/contacts/{contactId:guid}");
        Description(x => x.WithTags("Admin CRM"));
        PermissionsAll("crm.contacts.write");
    }

    public override async Task HandleAsync(LinkContactToPetRequest req, CancellationToken ct)
    {
        var command = new LinkCustomerContactToPetCommand(req.PetId, req.ContactId, req.RoleCodes, req.IsPrimary, req.CanPickUp, req.CanPay, req.ReceivesNotifications);
        var link = await command.ExecuteAsync(ct);
        if (link is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new LinkContactToPetResponse
        {
            PetId = link.PetId,
            ContactId = link.ContactId,
            ClientId = link.ClientId,
            ContactDisplayName = link.ContactDisplayName,
            RoleCodes = link.RoleCodes,
            IsPrimary = link.IsPrimary,
            CanPickUp = link.CanPickUp,
            CanPay = link.CanPay,
            ReceivesNotifications = link.ReceivesNotifications,
            Methods = link.Methods.Select(x => new ContactMethodSummaryResponse
            {
                Id = x.Id,
                MethodType = x.MethodType,
                DisplayValue = x.DisplayValue,
                IsPreferred = x.IsPreferred,
                VerificationStatus = x.VerificationStatus
            }).ToArray()
        }, ct);
    }
}

public sealed class LinkContactToPetRequestValidator : Validator<LinkContactToPetRequest>
{
    public LinkContactToPetRequestValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.ContactId).NotEmpty();
    }
}
