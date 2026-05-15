using System.Text;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Catalog.Api.Admin.Imports;

public sealed class GetCatalogOfferImportErrorsEndpoint(ICatalogOfferImportService importService)
    : Endpoint<GetCatalogOfferImportErrorsRequest>
{
    public override void Configure()
    {
        Get("/api/admin/catalog/imports/offers/{batchId:guid}/errors.csv");
        Description(x => x.WithTags("Admin Catalog"));
        PermissionsAll("catalog.read");
    }

    public override async Task HandleAsync(GetCatalogOfferImportErrorsRequest req, CancellationToken ct)
    {
        var result = await importService.ExportErrorsAsync(req.BatchId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        HttpContext.Response.ContentType = "text/csv; charset=utf-8";
        HttpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"catalog-offer-import-{req.BatchId:D}-errors.csv\"";
        await HttpContext.Response.WriteAsync(result.Value, Encoding.UTF8, ct);
    }
}
