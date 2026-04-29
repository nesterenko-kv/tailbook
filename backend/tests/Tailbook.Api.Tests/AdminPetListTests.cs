using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class AdminPetListTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminPetListTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_can_list_pets_with_pagination_and_filters()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var catalog = await TestApiHelpers.GetPetCatalogAsync(client);
        var firstClientId = await TestApiHelpers.CreateClientAsync(client, "Pet List Client A");
        var secondClientId = await TestApiHelpers.CreateClientAsync(client, "Pet List Client B");
        var firstPetId = await RegisterPetAsync(client, firstClientId, "Atlas", catalog);
        await RegisterPetAsync(client, secondClientId, "Biscuit", catalog);

        var firstPage = await client.GetFromJsonAsync<PagedPetsEnvelope>("/api/admin/pets?page=1&pageSize=1");
        Assert.NotNull(firstPage);
        Assert.Single(firstPage!.Items);
        Assert.True(firstPage.TotalCount >= 2);

        var clientFiltered = await client.GetFromJsonAsync<PagedPetsEnvelope>($"/api/admin/pets?clientId={firstClientId:D}");
        Assert.NotNull(clientFiltered);
        Assert.Contains(clientFiltered!.Items, x => x.Id == firstPetId);
        Assert.DoesNotContain(clientFiltered.Items, x => x.Name == "Biscuit");

        var searchFiltered = await client.GetFromJsonAsync<PagedPetsEnvelope>("/api/admin/pets?search=Atlas");
        Assert.NotNull(searchFiltered);
        Assert.Contains(searchFiltered!.Items, x => x.Id == firstPetId);

        var animalFiltered = await client.GetFromJsonAsync<PagedPetsEnvelope>($"/api/admin/pets?animalTypeCode={catalog.DogAnimalTypeCode}&breedId={catalog.SamoyedBreedId:D}");
        Assert.NotNull(animalFiltered);
        Assert.True(animalFiltered!.Items.Length >= 2);
    }

    [Fact]
    public async Task Admin_pet_list_requires_pet_read_permission()
    {
        using var anonymousClient = _factory.CreateClient();
        var anonymousResponse = await anonymousClient.GetAsync("/api/admin/pets");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        await _factory.SeedUserAsync("groomer-pets@test.local", "Pet Groomer", "Groomer123!", "groomer");
        var token = await _factory.LoginAsAsync("groomer-pets@test.local", "Groomer123!");
        using var groomerClient = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(groomerClient, token);

        var forbiddenResponse = await groomerClient.GetAsync("/api/admin/pets");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    private static async Task<Guid> RegisterPetAsync(HttpClient client, Guid clientId, string name, TestApiHelpers.PetCatalogSelection catalog)
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
