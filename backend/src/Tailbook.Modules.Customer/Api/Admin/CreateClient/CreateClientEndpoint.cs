using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Customer.Api.Admin.CreateClient;

public sealed class CreateClientEndpoint : Endpoint<CreateClientRequest, CreateClientResponse>
{
    public override void Configure()
    {
        Post("/api/admin/clients");
        Description(x => x.WithTags("Admin CRM"));
        PermissionsAll("crm.clients.write");
    }

    public override async Task HandleAsync(CreateClientRequest req, CancellationToken ct)
    {
        var command = new CreateCustomerClientCommand(req.DisplayName, req.Notes);
        var client = await command.ExecuteAsync(ct);
        await Send.ResponseAsync(new CreateClientResponse
        {
            Id = client.Id,
            DisplayName = client.DisplayName,
            Status = client.Status,
            Notes = client.Notes,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        }, StatusCodes.Status201Created, ct);
    }
}

public sealed class CreateClientRequestValidator : Validator<CreateClientRequest>
{
    public CreateClientRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
