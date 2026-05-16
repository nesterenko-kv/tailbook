using System.Net;
using System.Net.Http.Json;
using Tailbook.Api.Tests.Factories;
using Xunit;

namespace Tailbook.Modules.Catalog.Tests;

public sealed class RealDbCatalogFlowTests(RealDbWebApplicationFactory factory) : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Admin_can_create_package_offer_add_component_publish_and_read_offer_detail()
    {
        var token = await factory.LoginAsAsync(TestUsers.AdminEmail, TestUsers.AdminPassword);
        using var client = factory.CreateClient();
        RealDbWebApplicationFactory.SetBearer(client, token);

        var procedureResponse = await client.PostAsJsonAsync("/api/admin/catalog/procedures", new
        {
            code = "REAL_DB_TRIM",
            name = "Real DB Nail Trimming"
        });
        Assert.Equal(HttpStatusCode.Created, procedureResponse.StatusCode);
        var procedure = await procedureResponse.Content.ReadFromJsonAsync<ProcedureResponse>();
        Assert.NotNull(procedure);

        var createOfferResponse = await client.PostAsJsonAsync("/api/admin/catalog/offers", new
        {
            code = "REAL_DB_GROOMING",
            offerType = "Package",
            displayName = "Real DB Grooming"
        });
        Assert.Equal(HttpStatusCode.Created, createOfferResponse.StatusCode);
        var offer = await createOfferResponse.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.NotNull(offer);

        var createVersionResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offers/{offer.Id:D}/versions", new
        {
            offerId = offer.Id,
            changeNote = "Initial version"
        });
        Assert.Equal(HttpStatusCode.Created, createVersionResponse.StatusCode);
        var version = await createVersionResponse.Content.ReadFromJsonAsync<OfferVersionResponse>();
        Assert.NotNull(version);

        var addComponentResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version.Id:D}/components", new
        {
            versionId = version.Id,
            procedureId = procedure.Id,
            componentRole = "Included",
            sequenceNo = 1,
            defaultExpected = true
        });
        Assert.Equal(HttpStatusCode.Created, addComponentResponse.StatusCode);

        var publishResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version.Id:D}/publish", new { });
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);

        var detailResponse = await client.GetAsync($"/api/admin/catalog/offers/{offer.Id:D}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.NotNull(detail);
        Assert.Equal("Package", detail.OfferType);
        Assert.Single(detail.Versions);
        Assert.Single(detail.Versions[0].Components);
        Assert.Equal("Published", detail.Versions[0].Status);
        Assert.Equal("REAL_DB_TRIM", detail.Versions[0].Components[0].ProcedureCode);
    }

    private sealed class ProcedureResponse
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    private sealed class OfferResponse
    {
        public Guid Id { get; set; }
        public string OfferType { get; set; } = string.Empty;
        public OfferVersionResponse[] Versions { get; set; } = [];
    }

    private sealed class OfferVersionResponse
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public OfferVersionComponentResponse[] Components { get; set; } = [];
    }

    private sealed class OfferVersionComponentResponse
    {
        public string ProcedureCode { get; set; } = string.Empty;
    }
}
