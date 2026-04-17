using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Domain;

namespace Tailbook.Api.Tests;

public sealed class PricingDurationFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PricingDurationFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_can_create_rule_sets_publish_and_preview_quote_with_persisted_snapshots()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var catalog = await GetPetCatalogAsync(client);
        var clientId = await CreateClientAsync(client, "Quote Client");
        var petId = await RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);

        var offer = await CreateOfferAsync(client, "FULL_GROOM_QUOTE", "Package", "Full Groom Quote");
        var procedure = await CreateProcedureAsync(client, "FULL_GROOM_QUOTE_PROC", "Full Groom Quote Procedure");
        var version = await CreateVersionAsync(client, offer.Id);
        await AddComponentAsync(client, version.Id, procedure.Id);
        await PublishOfferVersionAsync(client, version.Id);

        var priceRuleSet = await CreatePriceRuleSetAsync(client);
        await AddPriceRuleAsync(client, priceRuleSet.Id, offer.Id, 100, 1400m, catalog.SamoyedBreedId, null);
        await PublishPriceRuleSetAsync(client, priceRuleSet.Id);

        var durationRuleSet = await CreateDurationRuleSetAsync(client);
        await AddDurationRuleAsync(client, durationRuleSet.Id, offer.Id, 100, 120, 10, 15, catalog.SamoyedBreedId, null);
        await PublishDurationRuleSetAsync(client, durationRuleSet.Id);

        var previewResponse = await client.PostAsJsonAsync("/api/admin/quotes/preview", new
        {
            petId,
            items = new[]
            {
                new { itemType = "Package", offerId = offer.Id }
            }
        });

        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var preview = await previewResponse.Content.ReadFromJsonAsync<PreviewQuoteResponse>();
        Assert.NotNull(preview);
        Assert.Equal(1400m, preview!.PriceSnapshot.TotalAmount);
        Assert.Equal(120, preview.DurationSnapshot.ServiceMinutes);
        Assert.Equal(145, preview.DurationSnapshot.ReservedMinutes);
        Assert.NotEqual(Guid.Empty, preview.PriceSnapshot.Id);
        Assert.NotEqual(Guid.Empty, preview.DurationSnapshot.Id);
        Assert.Single(preview.PriceSnapshot.Lines);
        Assert.Equal(3, preview.DurationSnapshot.Lines.Length);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(dbContext.Set<PriceSnapshot>().Any(x => x.Id == preview.PriceSnapshot.Id));
        Assert.True(dbContext.Set<DurationSnapshot>().Any(x => x.Id == preview.DurationSnapshot.Id));
    }

    [Fact]
    public async Task More_specific_breed_price_rule_wins_over_animal_type_fallback()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var catalog = await GetPetCatalogAsync(client);
        var clientId = await CreateClientAsync(client, "Specificity Client");
        var petId = await RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);

        var offer = await CreateOfferAsync(client, "BREED_RULE_PACKAGE", "Package", "Breed Rule Package");
        var procedure = await CreateProcedureAsync(client, "BREED_RULE_PACKAGE_PROC", "Breed Rule Package Procedure");
        var version = await CreateVersionAsync(client, offer.Id);
        await AddComponentAsync(client, version.Id, procedure.Id);
        await PublishOfferVersionAsync(client, version.Id);

        var priceRuleSet = await CreatePriceRuleSetAsync(client);
        await AddPriceRuleAsync(client, priceRuleSet.Id, offer.Id, 200, 1000m, null, catalog.DogAnimalTypeId);
        await AddPriceRuleAsync(client, priceRuleSet.Id, offer.Id, 100, 1500m, catalog.SamoyedBreedId, null);
        await PublishPriceRuleSetAsync(client, priceRuleSet.Id);

        var durationRuleSet = await CreateDurationRuleSetAsync(client);
        await AddDurationRuleAsync(client, durationRuleSet.Id, offer.Id, 100, 90, 5, 10, null, catalog.DogAnimalTypeId);
        await PublishDurationRuleSetAsync(client, durationRuleSet.Id);

        var previewResponse = await client.PostAsJsonAsync("/api/admin/quotes/preview", new
        {
            petId,
            items = new[]
            {
                new { itemType = "Package", offerId = offer.Id }
            }
        });

        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var preview = await previewResponse.Content.ReadFromJsonAsync<PreviewQuoteResponse>();
        Assert.NotNull(preview);
        Assert.Equal(1500m, preview!.PriceSnapshot.TotalAmount);
        Assert.Contains(preview.PriceSnapshot.Lines, x => x.Label.Contains("Samoyed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Groomer_cannot_access_pricing_or_quote_preview_endpoints()
    {
        await _factory.SeedUserAsync("groomer-pricing@test.local", "Groomer Pricing", "Groomer123!", "groomer");
        var token = await _factory.LoginAsAsync("groomer-pricing@test.local", "Groomer123!");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var pricingResponse = await client.GetAsync("/api/admin/pricing/rule-sets");
        Assert.Equal(HttpStatusCode.Forbidden, pricingResponse.StatusCode);

        var previewResponse = await client.PostAsJsonAsync("/api/admin/quotes/preview", new
        {
            petId = Guid.NewGuid(),
            items = new[]
            {
                new { itemType = "Package", offerId = Guid.NewGuid() }
            }
        });
        Assert.Equal(HttpStatusCode.Forbidden, previewResponse.StatusCode);
    }

    private static async Task<PetCatalogEnvelope> GetPetCatalogAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/admin/pets/catalog");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<PetCatalogEnvelope>();
        return payload!;
    }

    private static async Task<Guid> CreateClientAsync(HttpClient client, string displayName)
    {
        var response = await client.PostAsJsonAsync("/api/admin/clients", new { displayName });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CreateClientEnvelope>();
        return payload!.Id;
    }

    private static async Task<Guid> RegisterPetAsync(HttpClient client, Guid clientId, Guid breedId, string animalTypeCode, string coatTypeCode, string sizeCategoryCode)
    {
        var response = await client.PostAsJsonAsync("/api/admin/pets", new
        {
            clientId,
            name = "Milo",
            animalTypeCode,
            breedId,
            coatTypeCode,
            sizeCategoryCode,
            notes = "Quote preview pet"
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<PetEnvelope>();
        return payload!.Id;
    }

    private static async Task<OfferEnvelope> CreateOfferAsync(HttpClient client, string code, string offerType, string displayName)
    {
        var response = await client.PostAsJsonAsync("/api/admin/catalog/offers", new { code, offerType, displayName });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OfferEnvelope>())!;
    }

    private static async Task<ProcedureEnvelope> CreateProcedureAsync(HttpClient client, string code, string name)
    {
        var response = await client.PostAsJsonAsync("/api/admin/catalog/procedures", new { code, name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProcedureEnvelope>())!;
    }

    private static async Task<OfferVersionEnvelope> CreateVersionAsync(HttpClient client, Guid offerId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/catalog/offers/{offerId:D}/versions", new { offerId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OfferVersionEnvelope>())!;
    }

    private static async Task AddComponentAsync(HttpClient client, Guid versionId, Guid procedureId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{versionId:D}/components", new
        {
            versionId,
            procedureId,
            componentRole = "Included",
            sequenceNo = 1,
            defaultExpected = true
        });
        response.EnsureSuccessStatusCode();
    }

    private static async Task PublishOfferVersionAsync(HttpClient client, Guid versionId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{versionId:D}/publish", new { versionId });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<RuleSetEnvelope> CreatePriceRuleSetAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/admin/pricing/rule-sets", new { });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RuleSetEnvelope>())!;
    }

    private static async Task AddPriceRuleAsync(HttpClient client, Guid ruleSetId, Guid offerId, int priority, decimal fixedAmount, Guid? breedId, Guid? animalTypeId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/pricing/rule-sets/{ruleSetId:D}/rules", new
        {
            ruleSetId,
            offerId,
            priority,
            fixedAmount,
            currency = "UAH",
            breedId,
            animalTypeId
        });
        response.EnsureSuccessStatusCode();
    }

    private static async Task PublishPriceRuleSetAsync(HttpClient client, Guid ruleSetId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/pricing/rule-sets/{ruleSetId:D}/publish", new { ruleSetId });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<RuleSetEnvelope> CreateDurationRuleSetAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/admin/duration/rule-sets", new { });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RuleSetEnvelope>())!;
    }

    private static async Task AddDurationRuleAsync(HttpClient client, Guid ruleSetId, Guid offerId, int priority, int baseMinutes, int before, int after, Guid? breedId, Guid? animalTypeId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/duration/rule-sets/{ruleSetId:D}/rules", new
        {
            ruleSetId,
            offerId,
            priority,
            baseMinutes,
            bufferBeforeMinutes = before,
            bufferAfterMinutes = after,
            breedId,
            animalTypeId
        });
        response.EnsureSuccessStatusCode();
    }

    private static async Task PublishDurationRuleSetAsync(HttpClient client, Guid ruleSetId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/duration/rule-sets/{ruleSetId:D}/publish", new { ruleSetId });
        response.EnsureSuccessStatusCode();
    }

    private sealed class CreateClientEnvelope { public Guid Id { get; set; } }
    private sealed class PetEnvelope { public Guid Id { get; set; } }
    private sealed class OfferEnvelope { public Guid Id { get; set; } }
    private sealed class ProcedureEnvelope { public Guid Id { get; set; } }
    private sealed class OfferVersionEnvelope { public Guid Id { get; set; } }
    private sealed class RuleSetEnvelope { public Guid Id { get; set; } }

    private sealed class PetCatalogEnvelope
    {
        public AnimalTypeEnvelope[] AnimalTypes { get; set; } = [];
        public BreedEnvelope[] Breeds { get; set; } = [];
        public CoatTypeEnvelope[] CoatTypes { get; set; } = [];
        public SizeCategoryEnvelope[] SizeCategories { get; set; } = [];

        public Guid DogAnimalTypeId => AnimalTypes.Single(x => x.Code == "DOG").Id;
        public string DogAnimalTypeCode => AnimalTypes.Single(x => x.Code == "DOG").Code;
        public Guid SamoyedBreedId => Breeds.Single(x => x.Code == "SAMOYED").Id;
        public string DoubleCoatCode => CoatTypes.Single(x => x.Code == "DOUBLE_COAT").Code;
        public string LargeSizeCode => SizeCategories.Single(x => x.Code == "LARGE").Code;
    }

    private sealed class AnimalTypeEnvelope { public Guid Id { get; set; } public string Code { get; set; } = string.Empty; }
    private sealed class BreedEnvelope { public Guid Id { get; set; } public string Code { get; set; } = string.Empty; }
    private sealed class CoatTypeEnvelope { public string Code { get; set; } = string.Empty; }
    private sealed class SizeCategoryEnvelope { public string Code { get; set; } = string.Empty; }

    private sealed class PreviewQuoteResponse
    {
        public PriceSnapshotEnvelope PriceSnapshot { get; set; } = new();
        public DurationSnapshotEnvelope DurationSnapshot { get; set; } = new();

        public sealed class PriceSnapshotEnvelope
        {
            public Guid Id { get; set; }
            public decimal TotalAmount { get; set; }
            public PriceLineEnvelope[] Lines { get; set; } = [];
        }

        public sealed class PriceLineEnvelope
        {
            public string Label { get; set; } = string.Empty;
        }

        public sealed class DurationSnapshotEnvelope
        {
            public Guid Id { get; set; }
            public int ServiceMinutes { get; set; }
            public int ReservedMinutes { get; set; }
            public DurationLineEnvelope[] Lines { get; set; } = [];
        }

        public sealed class DurationLineEnvelope
        {
            public string Label { get; set; } = string.Empty;
        }
    }
}
