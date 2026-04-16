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
        var token = await _factory.LoginAsAsync("admin@test.local", "Admin12345!");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

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
    }

    private sealed class PetCatalogResponse
    {
        public BreedItem[] Breeds { get; set; } = [];
    }

    private sealed class BreedItem
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }
}
