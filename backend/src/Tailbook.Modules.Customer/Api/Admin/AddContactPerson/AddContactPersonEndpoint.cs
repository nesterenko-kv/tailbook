using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Customer.Api.Admin.AddContactPerson;

public sealed class AddContactPersonEndpoint(CustomerQueries customerQueries)
    : Endpoint<AddContactPersonRequest, AddContactPersonResponse>
{
    public override void Configure()
    {
        Post("/api/admin/clients/{clientId:guid}/contacts");
        Description(x => x.WithTags("Admin CRM"));
        PermissionsAll("crm.contacts.write");
    }

    public override async Task HandleAsync(AddContactPersonRequest req, CancellationToken ct)
    {
        var contact = await customerQueries.AddContactPersonAsync(req.ClientId, req.FirstName, req.LastName, req.Notes, req.TrustLevel, ct);
        if (contact is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(new AddContactPersonResponse
        {
            Id = contact.Id,
            ClientId = contact.ClientId,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            Notes = contact.Notes,
            TrustLevel = contact.TrustLevel
        }, StatusCodes.Status201Created, ct);
    }
}

public sealed class AddContactPersonRequest
{
    public Guid ClientId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Notes { get; set; }
    public string? TrustLevel { get; set; }
}

public sealed class AddContactPersonRequestValidator : Validator<AddContactPersonRequest>
{
    public AddContactPersonRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.TrustLevel).MaximumLength(32);
    }
}

public sealed class AddContactPersonResponse
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Notes { get; set; }
    public string TrustLevel { get; set; } = string.Empty;
}
