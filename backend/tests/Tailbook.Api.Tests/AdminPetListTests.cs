using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Auth;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;
using Tailbook.Api.Tests.TestSupport.Scenarios;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class AdminPetListTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Admin_can_list_pets_with_pagination_and_filters()
    {
        using var client = await factory.CreateAdminClientAsync();

        var catalog = await PetScenario.For(client).GetCatalogAsync();
        var customers = CustomerScenario.For(client);
        var firstClientId = await customers.CreateClientAsync("Pet List Client A");
        var secondClientId = await customers.CreateClientAsync("Pet List Client B");
        var firstPetId = await RegisterPetAsync(client, firstClientId, "Atlas", catalog);
        await RegisterPetAsync(client, secondClientId, "Biscuit", catalog);

        var firstPage = await client.GetFromJsonAsync<PagedPetsEnvelope>("/api/admin/pets?page=1&pageSize=1");
        Assert.NotNull(firstPage);
        Assert.Single(firstPage.Items);
        Assert.True(firstPage.TotalCount >= 2);

        var clientFiltered = await client.GetFromJsonAsync<PagedPetsEnvelope>($"/api/admin/pets?clientId={firstClientId:D}");
        Assert.NotNull(clientFiltered);
        Assert.Contains(clientFiltered.Items, x => x.Id == firstPetId);
        Assert.DoesNotContain(clientFiltered.Items, x => x.Name == "Biscuit");

        var searchFiltered = await client.GetFromJsonAsync<PagedPetsEnvelope>("/api/admin/pets?search=atlas");
        Assert.NotNull(searchFiltered);
        Assert.Contains(searchFiltered.Items, x => x.Id == firstPetId);

        var breedSearchFiltered = await client.GetFromJsonAsync<PagedPetsEnvelope>("/api/admin/pets?search=samoyed");
        Assert.NotNull(breedSearchFiltered);
        Assert.Contains(breedSearchFiltered.Items, x => x.Id == firstPetId);

        var animalFiltered = await client.GetFromJsonAsync<PagedPetsEnvelope>($"/api/admin/pets?animalTypeCode={catalog.DogAnimalTypeCode}&breedId={catalog.SamoyedBreedId:D}");
        Assert.NotNull(animalFiltered);
        Assert.True(animalFiltered.Items.Length >= 2);
    }

    [Fact]
    public async Task Admin_pet_list_requires_pet_read_permission()
    {
        using var anonymousClient = factory.CreateAnonymousClient();
        var anonymousResponse = await anonymousClient.GetAsync("/api/admin/pets");
        anonymousResponse.ShouldBeUnauthorized();

        using var groomerClient = await factory.CreateClientForRoleAsync(
            "groomer-pets@test.local",
            "Pet Groomer",
            TestUsers.GroomerPassword,
            "groomer");

        var forbiddenResponse = await groomerClient.GetAsync("/api/admin/pets");
        forbiddenResponse.ShouldBeForbidden();
    }

    private static async Task<Guid> RegisterPetAsync(HttpClient client, Guid clientId, string name, PetCatalogSelection catalog)
    {
        var response = await client.PostAsJsonAsync("/api/admin/pets", new
        {
            clientId,
            name,
            animalTypeCode = catalog.DogAnimalTypeCode,
            breedId = catalog.SamoyedBreedId,
            coatTypeCode = catalog.DoubleCoatCode,
            sizeCategoryCode = catalog.LargeSizeCode
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<IdEnvelope>();
        return payload!.Id;
    }

    private sealed class PagedPetsEnvelope
    {
        public PetListItemEnvelope[] Items { get; set; } = [];
        public int TotalCount { get; set; }
    }

    private sealed class PetListItemEnvelope
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class IdEnvelope
    {
        public Guid Id { get; set; }
    }
}
