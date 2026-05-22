using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Catalog.Api.Admin.ListProcedures;

public sealed class ListProceduresEndpoint(ICatalogReadService catalogReadService)
    : EndpointWithoutRequest<IReadOnlyCollection<ProcedureItemResponse>>
{
    public override void Configure()
    {
        Get("/api/admin/catalog/procedures");
        Description(x => x.WithTags("Admin Catalog"));
        PermissionsAll("catalog.read");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var procedures = await catalogReadService.ListProceduresAsync(ct);
        await Send.OkAsync(procedures.Select(x => new ProcedureItemResponse
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            IsActive = x.IsActive,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        }).ToArray(), ct);
    }
}