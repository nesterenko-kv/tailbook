using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Customer.Api.Admin.ListPetContactLinks;

public sealed class ListPetContactLinksEndpoint(ICustomerReadService customerReadService)
    : Endpoint<ListPetContactLinksRequest, ListPetContactLinksResponse>
{
    public override void Configure()
    {
        Get("/api/admin/pets/{petId:guid}/contacts");
        Description(x => x.WithTags("Admin CRM"));
        PermissionsAll("crm.contacts.read");
    }

    public override async Task HandleAsync(ListPetContactLinksRequest req, CancellationToken ct)
    {
        var links = await customerReadService.ListPetContactLinksAsync(req.PetId, req.ActorUserId, ct);
        if (links is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new ListPetContactLinksResponse
        {
            Items = links.Select(link => new PetContactLinkResponse
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
                Methods = link.Methods.Select(method => new ContactMethodResponse
                {
                    Id = method.Id,
                    MethodType = method.MethodType,
                    DisplayValue = method.DisplayValue,
                    IsPreferred = method.IsPreferred,
                    VerificationStatus = method.VerificationStatus,
                    Notes = method.Notes
                }).ToArray()
            }).ToArray()
        }, ct);
    }
}
