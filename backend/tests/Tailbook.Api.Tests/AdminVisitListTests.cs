using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class AdminVisitListTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminVisitListTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_can_list_visits_with_pagination_and_filters()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var catalog = await TestApiHelpers.GetPetCatalogAsync(client);
        var clientId = await TestApiHelpers.CreateClientAsync(client, "Visit List Client");
        var petId = await TestApiHelpers.RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);
        var offerId = await TestApiHelpers.CreateSchedulableOfferAsync(client, catalog.SamoyedBreedId);
        var firstGroomer = await TestApiHelpers.CreateSchedulableGroomerAsync(client);
        var secondGroomer = await TestApiHelpers.CreateSchedulableGroomerAsync(client);
        var firstAppointment = await TestApiHelpers.CreateAppointmentAsync(client, petId, firstGroomer.Id, offerId, DateTime.Parse("2026-04-24T07:00:00Z").ToUniversalTime());
        var secondAppointment = await TestApiHelpers.CreateAppointmentAsync(client, petId, secondGroomer.Id, offerId, DateTime.Parse("2026-04-25T07:00:00Z").ToUniversalTime());
        var firstVisit = await TestApiHelpers.CheckInAsync(client, firstAppointment.Id);
        var secondVisit = await TestApiHelpers.CheckInAsync(client, secondAppointment.Id);

        var firstPage = await client.GetFromJsonAsync<PagedVisitsEnvelope>("/api/admin/visits?page=1&pageSize=1");
        Assert.NotNull(firstPage);
        Assert.Single(firstPage!.Items);
        Assert.True(firstPage.TotalCount >= 2);

        var groomerFiltered = await client.GetFromJsonAsync<PagedVisitsEnvelope>($"/api/admin/visits?groomerId={firstGroomer.Id:D}");
        Assert.NotNull(groomerFiltered);
        Assert.Contains(groomerFiltered!.Items, x => x.Id == firstVisit.Id);
        Assert.DoesNotContain(groomerFiltered.Items, x => x.Id == secondVisit.Id);

        var dateFiltered = await client.GetFromJsonAsync<PagedVisitsEnvelope>("/api/admin/visits?fromUtc=2026-04-25T00:00:00Z&toUtc=2026-04-26T00:00:00Z");
        Assert.NotNull(dateFiltered);
        Assert.Contains(dateFiltered!.Items, x => x.Id == secondVisit.Id);
        Assert.DoesNotContain(dateFiltered.Items, x => x.Id == firstVisit.Id);

        var statusFilteredResponse = await client.GetAsync("/api/admin/visits?status=Open");
        var statusFilteredBody = await statusFilteredResponse.Content.ReadAsStringAsync();
        Assert.True(statusFilteredResponse.IsSuccessStatusCode, statusFilteredBody);
        var statusFiltered = await statusFilteredResponse.Content.ReadFromJsonAsync<PagedVisitsEnvelope>();
        Assert.NotNull(statusFiltered);
        Assert.Contains(statusFiltered!.Items, x => x.Id == firstVisit.Id);
        Assert.Contains(statusFiltered.Items, x => x.Id == secondVisit.Id);
    }

    [Fact]
    public async Task Admin_visit_list_requires_visit_read_permission()
    {
        using var anonymousClient = _factory.CreateClient();
        var anonymousResponse = await anonymousClient.GetAsync("/api/admin/visits");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        await _factory.SeedUserAsync("groomer-visit-list@test.local", "Visit List Groomer", "Groomer123!", "groomer");
        var token = await _factory.LoginAsAsync("groomer-visit-list@test.local", "Groomer123!");
        using var groomerClient = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(groomerClient, token);

        var forbiddenResponse = await groomerClient.GetAsync("/api/admin/visits");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    private sealed class PagedVisitsEnvelope
    {
        public VisitListItemEnvelope[] Items { get; set; } = [];
        public int TotalCount { get; set; }
    }

    private sealed class VisitListItemEnvelope
    {
        public Guid Id { get; set; }
    }
}
