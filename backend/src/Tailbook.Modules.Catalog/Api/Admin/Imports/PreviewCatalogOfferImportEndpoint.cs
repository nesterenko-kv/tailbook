using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Catalog.Api.Admin.Imports;

public sealed class PreviewCatalogOfferImportEndpoint(ICatalogOfferImportService importService)
    : Endpoint<PreviewCatalogOfferImportRequest, ImportBatchResponse>
{
    public override void Configure()
    {
        Post("/api/admin/catalog/imports/offers/preview");
        Description(x => x.WithTags("Admin Catalog"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(PreviewCatalogOfferImportRequest req, CancellationToken ct)
    {
        var result = await importService.PreviewAsync(new CatalogOfferImportPreviewInput(req.SourceName, req.CsvContent, req.ActorUserId), ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(ImportBatchResponseMapper.Map(result.Value), StatusCodes.Status201Created, ct);
    }
}
