using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Customer.Application;

namespace Tailbook.Modules.Customer.Api.Admin.AddContactMethod;

public sealed class AddContactMethodEndpoint(CustomerQueries customerQueries)
    : Endpoint<AddContactMethodRequest, AddContactMethodResponse>
{
    public override void Configure()
    {
        Post("/api/admin/contacts/{contactId:guid}/methods");
        Description(x => x.WithTags("Admin CRM"));
        PermissionsAll("crm.contacts.write");
    }

    public override async Task HandleAsync(AddContactMethodRequest req, CancellationToken ct)
    {
        var result = await customerQueries.AddContactMethodAsync(req.ContactId, req.MethodType, req.Value, req.DisplayValue, req.IsPreferred, req.VerificationStatus, req.Notes, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        var method = result.Value;
        await Send.ResponseAsync(new AddContactMethodResponse
        {
            Id = method.Id,
            MethodType = method.MethodType,
            DisplayValue = method.DisplayValue,
            IsPreferred = method.IsPreferred,
            VerificationStatus = method.VerificationStatus,
            Notes = method.Notes
        }, StatusCodes.Status201Created, ct);
    }
}

public sealed class AddContactMethodRequest
{
    public Guid ContactId { get; set; }
    public string MethodType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? DisplayValue { get; set; }
    public bool IsPreferred { get; set; }
    public string? VerificationStatus { get; set; }
    public string? Notes { get; set; }
}

public sealed class AddContactMethodRequestValidator : Validator<AddContactMethodRequest>
{
    public AddContactMethodRequestValidator()
    {
        RuleFor(x => x.ContactId).NotEmpty();
        RuleFor(x => x.MethodType).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(256);
        RuleFor(x => x.DisplayValue).MaximumLength(256);
        RuleFor(x => x.VerificationStatus).MaximumLength(32);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class AddContactMethodResponse
{
    public Guid Id { get; set; }
    public string MethodType { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
