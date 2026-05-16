using System.Net.Http.Json;
using Tailbook.Api.Tests.Factories;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class AdminVisitListTests(RealDbWebApplicationFactory factory)
    : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Admin_can_list_visits_with_pagination_and_filters()
    {
        using var client = await factory.CreateAdminClientAsync();

        var pet = await PetScenario.For(client).CreateSchedulablePetAsync("Visit List Client");
        var offerId = await CatalogScenario.For(client).CreateSchedulableOfferAsync(pet.Catalog.SamoyedBreedId);
        var staff = StaffScenario.For(client);
        var firstGroomer = await staff.CreateSchedulableGroomerAsync("Visit List Groomer A");
        var secondGroomer = await staff.CreateSchedulableGroomerAsync("Visit List Groomer B");
        var booking = AdminBookingApi.For(client);
        var firstAppointment = await booking.CreateAppointmentAsync(
            pet.PetId,
            firstGroomer.Id,
            offerId,
            ApiClientExtensions.UtcDateTime("2026-04-24T07:00:00Z"));
        var secondAppointment = await booking.CreateAppointmentAsync(
            pet.PetId,
            secondGroomer.Id,
            offerId,
            ApiClientExtensions.UtcDateTime("2026-04-25T07:00:00Z"));
        var visits = AdminVisitsApi.For(client);
        var firstVisit = await visits.CheckInAsync(firstAppointment.Id);
        var secondVisit = await visits.CheckInAsync(secondAppointment.Id);

        var firstPage = await client.GetFromJsonAsync<PagedVisitsEnvelope>("/api/admin/visits?page=1&pageSize=1");
        Assert.NotNull(firstPage);
        Assert.Single(firstPage.Items);
        Assert.True(firstPage.TotalCount >= 2);

        var groomerFiltered = await client.GetFromJsonAsync<PagedVisitsEnvelope>($"/api/admin/visits?groomerId={firstGroomer.Id:D}");
        Assert.NotNull(groomerFiltered);
        Assert.Contains(groomerFiltered.Items, x => x.Id == firstVisit.Id);
        Assert.DoesNotContain(groomerFiltered.Items, x => x.Id == secondVisit.Id);

        var dateFiltered = await client.GetFromJsonAsync<PagedVisitsEnvelope>("/api/admin/visits?from=2026-04-25T00:00:00Z&to=2026-04-26T00:00:00Z");
        Assert.NotNull(dateFiltered);
        Assert.Contains(dateFiltered.Items, x => x.Id == secondVisit.Id);
        Assert.DoesNotContain(dateFiltered.Items, x => x.Id == firstVisit.Id);

        var statusFilteredResponse = await client.GetAsync("/api/admin/visits?status=Open");
        var statusFilteredBody = await statusFilteredResponse.Content.ReadAsStringAsync();
        Assert.True(statusFilteredResponse.IsSuccessStatusCode, statusFilteredBody);
        var statusFiltered = await statusFilteredResponse.Content.ReadFromJsonAsync<PagedVisitsEnvelope>();
        Assert.NotNull(statusFiltered);
        Assert.Contains(statusFiltered.Items, x => x.Id == firstVisit.Id);
        Assert.Contains(statusFiltered.Items, x => x.Id == secondVisit.Id);

        var searchFiltered = await client.GetFromJsonAsync<PagedVisitsEnvelope>("/api/admin/visits?search=schedulable");
        Assert.NotNull(searchFiltered);
        Assert.Contains(searchFiltered.Items, x => x.Id == firstVisit.Id);
        Assert.Contains(searchFiltered.Items, x => x.Id == secondVisit.Id);
    }

    [Fact]
    public async Task Admin_visit_list_requires_visit_read_permission()
    {
        using var anonymousClient = factory.CreateAnonymousClient();
        var anonymousResponse = await anonymousClient.GetAsync("/api/admin/visits");
        anonymousResponse.ShouldBeUnauthorized();

        using var groomerClient = await factory.CreateClientForRoleAsync(
            "groomer-visit-list@test.local",
            "Visit List Groomer",
            TestUsers.GroomerPassword,
            "groomer");

        var forbiddenResponse = await groomerClient.GetAsync("/api/admin/visits");
        forbiddenResponse.ShouldBeForbidden();
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
