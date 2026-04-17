using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class PetValidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PetValidationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_pet_rejects_breed_that_does_not_belong_to_selected_animal_type()
    {
        var client = await CreateAuthorizedClientAsync();
        var catalog = await client.GetFromJsonAsync<PetCatalogResponse>("/api/admin/pets/catalog");
        Assert.NotNull(catalog);
        var samoyedBreedId = catalog!.Breeds.Single(x => x.Code == "SAMOYED").Id;

        var response = await client.PostAsJsonAsync("/api/admin/pets", new
        {
            name = "Broken",
            animalTypeCode = "CAT",
            breedId = samoyedBreedId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Breed must belong to the selected animal type", content);
    }

    [Fact]
    public async Task Register_pet_rejects_coat_type_incompatible_with_selected_breed()
    {
        var client = await CreateAuthorizedClientAsync();
        var catalog = await client.GetFromJsonAsync<PetCatalogResponse>("/api/admin/pets/catalog");
        Assert.NotNull(catalog);

        var samoyedBreedId = catalog!.Breeds.Single(x => x.Code == "SAMOYED").Id;
        var response = await client.PostAsJsonAsync("/api/admin/pets", new
        {
            name = "Mismatch Coat",
            animalTypeCode = "DOG",
            breedId = samoyedBreedId,
            coatTypeCode = "CURLY_COAT"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Coat type 'Curly Coat' is not allowed for breed 'Samoyed'", content);
    }

    [Fact]
    public async Task Update_pet_rejects_coat_type_incompatible_with_selected_breed()
    {
        var client = await CreateAuthorizedClientAsync();
        var catalog = await client.GetFromJsonAsync<PetCatalogResponse>("/api/admin/pets/catalog");
        Assert.NotNull(catalog);

        var clientResponse = await client.PostAsJsonAsync("/api/admin/clients", new { displayName = "Pet validation owner" });
        clientResponse.EnsureSuccessStatusCode();
        var createdClient = await clientResponse.Content.ReadFromJsonAsync<IdEnvelope>();
        Assert.NotNull(createdClient);

        var samoyedBreedId = catalog!.Breeds.Single(x => x.Code == "SAMOYED").Id;
        var registerResponse = await client.PostAsJsonAsync("/api/admin/pets", new
        {
            clientId = createdClient!.Id,
            name = "Cloud",
            animalTypeCode = "DOG",
            breedId = samoyedBreedId,
            coatTypeCode = "DOUBLE_COAT"
        });
        registerResponse.EnsureSuccessStatusCode();
        var createdPet = await registerResponse.Content.ReadFromJsonAsync<IdEnvelope>();
        Assert.NotNull(createdPet);

        var updateResponse = await client.PatchAsJsonAsync($"/api/admin/pets/{createdPet!.Id:D}", new
        {
            id = createdPet.Id,
            name = "Cloud",
            animalTypeCode = "DOG",
            breedId = samoyedBreedId,
            coatTypeCode = "CURLY_COAT"
        });

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
        var content = await updateResponse.Content.ReadAsStringAsync();
        Assert.Contains("Coat type 'Curly Coat' is not allowed for breed 'Samoyed'", content);
    }

    [Fact]
    public async Task Pet_catalog_returns_breed_allowed_coat_type_ids_for_ui_filtering()
    {
        var client = await CreateAuthorizedClientAsync();
        var catalog = await client.GetFromJsonAsync<PetCatalogResponse>("/api/admin/pets/catalog");
        Assert.NotNull(catalog);

        var doubleCoatId = catalog!.CoatTypes.Single(x => x.Code == "DOUBLE_COAT").Id;
        var curlyCoatId = catalog.CoatTypes.Single(x => x.Code == "CURLY_COAT").Id;
        var shortCoatId = catalog.CoatTypes.Single(x => x.Code == "SHORT_COAT").Id;
        var longCoatId = catalog.CoatTypes.Single(x => x.Code == "LONG_COAT").Id;

        var samoyed = catalog.Breeds.Single(x => x.Code == "SAMOYED");
        Assert.Contains(doubleCoatId, samoyed.AllowedCoatTypeIds);
        Assert.DoesNotContain(curlyCoatId, samoyed.AllowedCoatTypeIds);
        Assert.DoesNotContain(shortCoatId, samoyed.AllowedCoatTypeIds);

        var miniaturePoodle = catalog.Breeds.Single(x => x.Code == "POODLE_MINIATURE");
        Assert.Contains(curlyCoatId, miniaturePoodle.AllowedCoatTypeIds);

        var britishShorthair = catalog.Breeds.Single(x => x.Code == "BRITISH_SHORTHAIR");
        Assert.Contains(shortCoatId, britishShorthair.AllowedCoatTypeIds);

        var siberianCat = catalog.Breeds.Single(x => x.Code == "SIBERIAN_CAT");
        Assert.Contains(longCoatId, siberianCat.AllowedCoatTypeIds);
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);
        return client;
    }

    private sealed class PetCatalogResponse
    {
        public BreedItem[] Breeds { get; set; } = [];
        public CoatTypeItem[] CoatTypes { get; set; } = [];
    }

    private sealed class BreedItem
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public Guid[] AllowedCoatTypeIds { get; set; } = [];
    }

    private sealed class CoatTypeItem
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    private sealed class IdEnvelope
    {
        public Guid Id { get; set; }
    }
}
