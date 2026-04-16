using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Catalog.Application;

namespace Tailbook.Modules.Catalog.Api.Admin.ListProcedures;

public sealed class ListProceduresEndpoint(ICurrentUser currentUser, ICatalogAccessPolicy accessPolicy, CatalogQueries catalogQueries)
    : EndpointWithoutRequest<IReadOnlyCollection<ProcedureItemResponse>>
{
    public override void Configure()
    {
        Get("/api/admin/catalog/procedures");
        Description(x => x.WithTags("Admin Catalog"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanReadCatalog(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var procedures = await catalogQueries.ListProceduresAsync(ct);
        await Send.OkAsync(procedures.Select(x => new ProcedureItemResponse
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            IsActive = x.IsActive,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc
        }).ToArray(), ct);
    }
}

public sealed class ProcedureItemResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
