using System.Net.Http.Json;
using Tailbook.Api.Tests.Factories;
using Xunit;

namespace Tailbook.Modules.VisitOperations.Tests;

public sealed class VisitValidationTests(RealDbWebApplicationFactory factory)
    : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Visit_cannot_be_completed_until_default_expected_components_are_accounted_for()
    {
        using var client = await CreateAuthorizedClientAsync();
        var visit = await CreateOpenVisitAsync(client);

        var response = await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/complete", new { visitId = visit.Id });

        response.ShouldBeBadRequest();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("must be performed or skipped before completion", body);
    }

    [Fact]
    public async Task Visit_adjustment_cannot_make_final_total_negative()
    {
        using var client = await CreateAuthorizedClientAsync();
        var visit = await CreateOpenVisitAsync(client);

        var response = await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/adjustments", new
        {
            visitId = visit.Id,
            sign = -1,
            amount = 5000m,
            reasonCode = "INVALID_REDUCTION"
        });

        response.ShouldBeBadRequest();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Visit final total cannot be negative", body);
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync()
        => await factory.CreateAdminClientAsync();

    private static async Task<VisitEnvelope> CreateOpenVisitAsync(HttpClient client)
    {
        var scenario = await VisitScenario
            .For(client)
            .WithSchedulablePet("Visit Validation Client")
            .WithVisitReadyOffer()
            .WithAvailableGroomer()
            .CreateAsync();

        return await scenario.CheckInAsync();
    }
}
