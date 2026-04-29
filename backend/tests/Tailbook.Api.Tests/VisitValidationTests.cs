using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class VisitValidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public VisitValidationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Visit_cannot_be_completed_until_default_expected_components_are_accounted_for()
    {
        var client = await CreateAuthorizedClientAsync();
        var visit = await CreateOpenVisitAsync(client);

        var response = await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/complete", new { visitId = visit.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("must be performed or skipped before completion", body);
    }

    [Fact]
    public async Task Visit_adjustment_cannot_make_final_total_negative()
    {
        var client = await CreateAuthorizedClientAsync();
        var visit = await CreateOpenVisitAsync(client);

        var response = await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/adjustments", new
        {
            visitId = visit.Id,
            sign = -1,
            amount = 5000m,
            reasonCode = "INVALID_REDUCTION"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Visit final total cannot be negative", body);
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);
        return client;
    }

    private static async Task<TestApiHelpers.VisitEnvelope> CreateOpenVisitAsync(HttpClient client)
    {
        var catalog = await TestApiHelpers.GetPetCatalogAsync(client);
        var clientId = await TestApiHelpers.CreateClientAsync(client, "Visit Validation Client");
        var petId = await TestApiHelpers.RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);
        var offerId = await TestApiHelpers.CreateSchedulableOfferAsync(client, catalog.SamoyedBreedId);
        var groomer = await TestApiHelpers.CreateSchedulableGroomerAsync(client);
        var appointment = await TestApiHelpers.CreateAppointmentAsync(client, petId, groomer.Id, offerId, DateTime.Parse("2026-04-24T07:00:00Z").ToUniversalTime());
        return await TestApiHelpers.CheckInAsync(client, appointment.Id);
    }
}
