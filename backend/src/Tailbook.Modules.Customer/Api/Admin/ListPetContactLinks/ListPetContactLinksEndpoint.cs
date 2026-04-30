using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Customer.Api.Admin.ListPetContactLinks;

public sealed class ListPetContactLinksEndpoint(CustomerQueries customerQueries)
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
        var links = await customerQueries.ListPetContactLinksAsync(req.PetId, req.ActorUserId, ct);
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

public sealed class ListPetContactLinksRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid PetId { get; set; }
}

public sealed class ListPetContactLinksResponse
{
    public IReadOnlyCollection<PetContactLinkResponse> Items { get; set; } = [];
}

public sealed class PetContactLinkResponse
{
    public Guid PetId { get; set; }
    public Guid ContactId { get; set; }
    public Guid ClientId { get; set; }
    public string ContactDisplayName { get; set; } = string.Empty;
    public IReadOnlyCollection<string> RoleCodes { get; set; } = [];
    public bool IsPrimary { get; set; }
    public bool CanPickUp { get; set; }
    public bool CanPay { get; set; }
    public bool ReceivesNotifications { get; set; }
    public IReadOnlyCollection<ContactMethodResponse> Methods { get; set; } = [];
}

public sealed class ContactMethodResponse
{
    public Guid Id { get; set; }
    public string MethodType { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
