using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class CatalogImportFlowTests(RealDbWebApplicationFactory factory) : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Admin_can_preview_catalog_offer_import_and_list_history()
    {
        var token = await factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = factory.CreateClient();
        RealDbWebApplicationFactory.SetBearer(client, token);

        var response = await client.PostAsJsonAsync("/api/admin/catalog/imports/offers/preview", new
        {
            sourceName = "offers-valid.csv",
            csvContent = "ExternalId,Code,DisplayName,PriceAmount,ServiceMinutes,ReservedMinutes\nimp-1,IMPORT_BATH,Bath Import,35.00,45,60\nimp-2,IMPORT_NAILS,Nail Import,18.50,15,15"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var batch = await response.Content.ReadFromJsonAsync<ImportBatchResponse>();
        Assert.NotNull(batch);
        Assert.Equal("catalog.offers", batch.Domain);
        Assert.Equal("Validated", batch.Status);
        Assert.Equal(2, batch.TotalRows);
        Assert.Equal(2, batch.ValidRows);
        Assert.Equal(0, batch.ErrorRows);
        Assert.Empty(batch.Issues);

        var history = await client.GetFromJsonAsync<ImportBatchSummaryResponse[]>("/api/admin/catalog/imports/offers");
        Assert.NotNull(history);
        Assert.Contains(history, x => x.Id == batch.Id && x.SourceName == "offers-valid.csv");
    }

    [Fact]
    public async Task Admin_can_export_catalog_offer_import_row_errors()
    {
        var token = await factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = factory.CreateClient();
        RealDbWebApplicationFactory.SetBearer(client, token);

        var createOfferResponse = await client.PostAsJsonAsync("/api/admin/catalog/offers", new
        {
            code = "IMPORT_EXISTING",
            offerType = "StandaloneService",
            displayName = "Existing Import Offer"
        });
        Assert.Equal(HttpStatusCode.Created, createOfferResponse.StatusCode);

        var previewResponse = await client.PostAsJsonAsync("/api/admin/catalog/imports/offers/preview", new
        {
            sourceName = "offers-invalid.csv",
            csvContent = "ExternalId,Code,DisplayName,PriceAmount,ServiceMinutes,ReservedMinutes\nimp-existing,IMPORT_EXISTING,Existing Duplicate,10,20,20\nimp-bad,IMPORT_BAD,,12,30,10"
        });

        Assert.Equal(HttpStatusCode.Created, previewResponse.StatusCode);
        var batch = await previewResponse.Content.ReadFromJsonAsync<ImportBatchResponse>();
        Assert.NotNull(batch);
        Assert.Equal("Invalid", batch.Status);
        Assert.Equal(2, batch.TotalRows);
        Assert.Equal(0, batch.ValidRows);
        Assert.Equal(2, batch.ErrorRows);
        Assert.Contains(batch.Issues, x => x.RowNumber == 2 && x.Code == "identifier.exists");
        Assert.Contains(batch.Issues, x => x.RowNumber == 3 && x.Code == "field.required");
        Assert.Contains(batch.Issues, x => x.RowNumber == 3 && x.Code == "duration.reserved_less_than_service");

        var exportResponse = await client.GetAsync($"/api/admin/catalog/imports/offers/{batch.Id:D}/errors.csv");
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.Equal("text/csv", exportResponse.Content.Headers.ContentType?.MediaType);
        var csv = await exportResponse.Content.ReadAsStringAsync();
        Assert.Contains("RowNumber,Field,Code,Message", csv, StringComparison.Ordinal);
        Assert.Contains("identifier.exists", csv, StringComparison.Ordinal);
        Assert.Contains("duration.reserved_less_than_service", csv, StringComparison.Ordinal);
    }

    private class ImportBatchSummaryResponse
    {
        public Guid Id { get; set; }
        public string Domain { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int ErrorRows { get; set; }
    }

    private sealed class ImportBatchResponse : ImportBatchSummaryResponse
    {
        public ImportRowIssueResponse[] Issues { get; set; } = [];
    }

    private sealed class ImportRowIssueResponse
    {
        public int RowNumber { get; set; }
        public string Code { get; set; } = string.Empty;
    }
}
