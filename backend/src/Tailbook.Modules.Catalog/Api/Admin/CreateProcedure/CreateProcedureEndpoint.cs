using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateProcedure;

public sealed class CreateProcedureEndpoint : Endpoint<CreateProcedureRequest, CreateProcedureResponse>
{
    public override void Configure()
    {
        Post("/api/admin/catalog/procedures");
        Description(x => x.WithTags("Admin Catalog"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(CreateProcedureRequest req, CancellationToken ct)
    {
        var command = new CreateCatalogProcedureCommand(req.Code, req.Name);
        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        var procedure = result.Value;
        await Send.ResponseAsync(new CreateProcedureResponse
        {
            Id = procedure.Id,
            Code = procedure.Code,
            Name = procedure.Name,
            IsActive = procedure.IsActive,
            CreatedAt = procedure.CreatedAt,
            UpdatedAt = procedure.UpdatedAt
        }, StatusCodes.Status201Created, ct);
    }
}
