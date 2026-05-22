using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Catalog.Api.Admin.Imports;

public sealed class ListCatalogOfferImportsEndpoint(ICatalogOfferImportService importService)
    : EndpointWithoutRequest<IReadOnlyCollection<ImportBatchSummaryResponse>>
{
    public override void Configure()
    {
        Get("/api/admin/catalog/imports/offers");
        Description(x => x.WithTags("Admin Catalog"));
        PermissionsAll("catalog.read");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var batches = await importService.ListAsync(ct);
        await Send.OkAsync(batches.Select(ImportBatchResponseMapper.Map).ToArray(), ct);
    }
}
