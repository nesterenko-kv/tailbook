using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;

namespace Tailbook.Api.Tests.TestSupport.Scenarios;

public sealed class PetScenario(HttpClient client)
{
    public static PetScenario For(HttpClient client)
        => new(client);

    public async Task<PetCatalogSelection> GetCatalogAsync()
    {
        var response = await client.GetAsync("/api/admin/pets/catalog");
        response.EnsureSuccessStatusCode();
        return (await response.ReadRequiredJsonAsync<PetCatalogEnvelope>()).SelectSamoyed();
    }

    public async Task<Guid> RegisterPetAsync(
        Guid clientId,
        PetCatalogSelection catalog,
        string name = "Milo",
        string? notes = null)
    {
        var response = await client.PostAsJsonAsync("/api/admin/pets", new
        {
            clientId,
            name,
            animalTypeCode = catalog.DogAnimalTypeCode,
            breedId = catalog.SamoyedBreedId,
            coatTypeCode = catalog.DoubleCoatCode,
            sizeCategoryCode = catalog.LargeSizeCode,
            notes
        });
        response.EnsureSuccessStatusCode();
        return (await response.ReadRequiredJsonAsync<PetEnvelope>()).Id;
    }

    public async Task<SchedulablePet> CreateSchedulablePetAsync(string clientDisplayName, string? petNotes = null)
    {
        var catalog = await GetCatalogAsync();
        var clientId = await CustomerScenario.For(client).CreateClientAsync(clientDisplayName);
        var petId = await RegisterPetAsync(clientId, catalog, notes: petNotes);
        return new SchedulablePet(clientId, petId, catalog);
    }
}

public sealed record SchedulablePet(Guid ClientId, Guid PetId, PetCatalogSelection Catalog);
