using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Customer.Application;

namespace Tailbook.Modules.Customer.Api.Admin.LinkContactToPet;

public sealed class LinkContactToPetEndpoint(ICurrentUser currentUser, ICustomerAccessPolicy accessPolicy, CustomerQueries customerQueries)
    : Endpoint<LinkContactToPetRequest, LinkContactToPetResponse>
{
    public override void Configure()
    {
        Post("/api/admin/pets/{petId:guid}/contacts/{contactId:guid}");
        Description(x => x.WithTags("Admin CRM"));
    }

    public override async Task HandleAsync(LinkContactToPetRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanWriteContacts(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var link = await customerQueries.LinkContactToPetAsync(req.PetId, req.ContactId, req.RoleCodes, req.IsPrimary, req.CanPickUp, req.CanPay, req.ReceivesNotifications, ct);
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

public sealed class LinkContactToPetRequest
{
    public Guid PetId { get; set; }
    public Guid ContactId { get; set; }
    public IReadOnlyCollection<string> RoleCodes { get; set; } = [];
    public bool IsPrimary { get; set; }
    public bool CanPickUp { get; set; }
    public bool CanPay { get; set; }
    public bool ReceivesNotifications { get; set; } = true;
}

public sealed class LinkContactToPetRequestValidator : Validator<LinkContactToPetRequest>
{
    public LinkContactToPetRequestValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.ContactId).NotEmpty();
    }
}

public sealed class LinkContactToPetResponse
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
    public IReadOnlyCollection<ContactMethodSummaryResponse> Methods { get; set; } = [];
}

public sealed class ContactMethodSummaryResponse
{
    public Guid Id { get; set; }
    public string MethodType { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
}
