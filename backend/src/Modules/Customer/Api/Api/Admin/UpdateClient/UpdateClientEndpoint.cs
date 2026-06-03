using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Customer.Api.Admin.UpdateClient;

public sealed class UpdateClientEndpoint : Endpoint<UpdateClientRequest, UpdateClientResponse>
{
    public override void Configure()
    {
        Put("/api/admin/clients/{id:guid}");
        Description(x => x.WithTags("Admin CRM"));
        PermissionsAll("crm.clients.write");
    }

    public override async Task HandleAsync(UpdateClientRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var command = new UpdateCustomerClientCommand(id, req.DisplayName, req.Notes);
        var result = await command.ExecuteAsync(ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(new UpdateClientResponse
        {
            Id = result.Id,
            DisplayName = result.DisplayName,
            Status = result.Status,
            Notes = result.Notes,
            CreatedAt = result.CreatedAt,
            UpdatedAt = result.UpdatedAt
        }, cancellation: ct);
    }
}
