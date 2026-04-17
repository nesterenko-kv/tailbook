using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class CatalogFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CatalogFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_can_create_package_offer_add_component_publish_and_read_offer_detail()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var procedureResponse = await client.PostAsJsonAsync("/api/admin/catalog/procedures", new
        {
            code = "NAIL_TRIM",
            name = "Nail Trimming"
        });
        Assert.Equal(HttpStatusCode.Created, procedureResponse.StatusCode);
        var procedure = await procedureResponse.Content.ReadFromJsonAsync<ProcedureResponse>();
        Assert.NotNull(procedure);

        var createOfferResponse = await client.PostAsJsonAsync("/api/admin/catalog/offers", new
        {
            code = "FULL_GROOMING",
            offerType = "Package",
            displayName = "Full Grooming"
        });
        Assert.Equal(HttpStatusCode.Created, createOfferResponse.StatusCode);
        var offer = await createOfferResponse.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.NotNull(offer);

        var createVersionResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offers/{offer!.Id:D}/versions", new
        {
            offerId = offer.Id,
            changeNote = "Initial package composition"
        });
        Assert.Equal(HttpStatusCode.Created, createVersionResponse.StatusCode);
        var version = await createVersionResponse.Content.ReadFromJsonAsync<OfferVersionResponse>();
        Assert.NotNull(version);

        var addComponentResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version!.Id:D}/components", new
        {
            versionId = version.Id,
            procedureId = procedure!.Id,
            componentRole = "Included",
            sequenceNo = 1,
            defaultExpected = true
        });
        Assert.Equal(HttpStatusCode.Created, addComponentResponse.StatusCode);

        var publishResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version.Id:D}/publish", new { versionId = version.Id });
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);

        var detailResponse = await client.GetAsync($"/api/admin/catalog/offers/{offer.Id:D}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.NotNull(detail);
        Assert.Equal("Package", detail!.OfferType);
        Assert.Single(detail.Versions);
        Assert.Single(detail.Versions[0].Components);
        Assert.Equal("Published", detail.Versions[0].Status);
        Assert.Equal("NAIL_TRIM", detail.Versions[0].Components[0].ProcedureCode);
    }

    [Fact]
    public async Task Published_offer_version_is_immutable_for_components()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var procedureA = await CreateProcedureAsync(client, "EAR_CLEAN", "Ear Cleaning");
        var procedureB = await CreateProcedureAsync(client, "BRUSHING", "Brushing");
        var offer = await CreateOfferAsync(client, "EXPRESS_PACKAGE", "Package", "Express Package");
        var version = await CreateVersionAsync(client, offer.Id);

        var addInitialComponent = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version.Id:D}/components", new
        {
            versionId = version.Id,
            procedureId = procedureA.Id,
            componentRole = "Included",
            sequenceNo = 1,
            defaultExpected = true
        });
        Assert.Equal(HttpStatusCode.Created, addInitialComponent.StatusCode);

        var publishResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version.Id:D}/publish", new { versionId = version.Id });
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);

        var addAfterPublishResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version.Id:D}/components", new
        {
            versionId = version.Id,
            procedureId = procedureB.Id,
            componentRole = "Included",
            sequenceNo = 2,
            defaultExpected = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, addAfterPublishResponse.StatusCode);
    }

    [Fact]
    public async Task Groomer_cannot_access_admin_catalog_endpoints()
    {
        await _factory.SeedUserAsync("groomer-catalog@test.local", "Groomer", "Groomer123!", "groomer");
        var token = await _factory.LoginAsAsync("groomer-catalog@test.local", "Groomer123!");

        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync("/api/admin/catalog/offers");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Standalone_service_version_cannot_accept_package_components()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var procedure = await CreateProcedureAsync(client, "DEMATTING", "Dematting");
        var offer = await CreateOfferAsync(client, "DEMATTING_ONLY", "StandaloneService", "Dematting Only");
        var version = await CreateVersionAsync(client, offer.Id);

        var response = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version.Id:D}/components", new
        {
            versionId = version.Id,
            procedureId = procedure.Id,
            componentRole = "Included",
            sequenceNo = 1,
            defaultExpected = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<ProcedureResponse> CreateProcedureAsync(HttpClient client, string code, string name)
    {
        var response = await client.PostAsJsonAsync("/api/admin/catalog/procedures", new { code, name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProcedureResponse>())!;
    }

    private static async Task<OfferResponse> CreateOfferAsync(HttpClient client, string code, string offerType, string displayName)
    {
        var response = await client.PostAsJsonAsync("/api/admin/catalog/offers", new { code, offerType, displayName });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OfferResponse>())!;
    }

    private static async Task<OfferVersionResponse> CreateVersionAsync(HttpClient client, Guid offerId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/catalog/offers/{offerId:D}/versions", new { offerId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OfferVersionResponse>())!;
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
