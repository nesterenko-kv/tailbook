using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Customer.Api.Admin.AddContactMethod;

public sealed class AddContactMethodEndpoint : Endpoint<AddContactMethodRequest, AddContactMethodResponse>
{
    public override void Configure()
    {
        Post("/api/admin/contacts/{contactId:guid}/methods");
        Description(x => x.WithTags("Admin CRM"));
        PermissionsAll("crm.contacts.write");
    }

    public override async Task HandleAsync(AddContactMethodRequest req, CancellationToken ct)
    {
        var command = new AddCustomerContactMethodCommand(
            req.ContactId,
            req.MethodType,
            req.Value,
            req.DisplayValue,
            req.IsPreferred,
            req.VerificationStatus,
            req.Notes
        );

        var result = await command.ExecuteAsync(ct);
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
