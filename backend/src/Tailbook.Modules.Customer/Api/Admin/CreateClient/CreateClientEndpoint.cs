using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Customer.Api.Admin.CreateClient;

public sealed class CreateClientEndpoint(CustomerQueries customerQueries)
    : Endpoint<CreateClientRequest, CreateClientResponse>
{
    public override void Configure()
    {
        Post("/api/admin/clients");
        Description(x => x.WithTags("Admin CRM"));
        PermissionsAll("crm.clients.write");
    }

    public override async Task HandleAsync(CreateClientRequest req, CancellationToken ct)
    {
        var client = await customerQueries.CreateClientAsync(req.DisplayName, req.Notes, ct);
        await Send.ResponseAsync(new CreateClientResponse
        {
            Id = client.Id,
            DisplayName = client.DisplayName,
            Status = client.Status,
            Notes = client.Notes,
            CreatedAtUtc = client.CreatedAtUtc,
            UpdatedAtUtc = client.UpdatedAtUtc
        }, StatusCodes.Status201Created, ct);
    }
}

public sealed class CreateClientRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class CreateClientRequestValidator : Validator<CreateClientRequest>
{
    public CreateClientRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class CreateClientResponse
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
