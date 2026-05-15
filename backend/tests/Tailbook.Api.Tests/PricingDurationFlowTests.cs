using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.Api.Tests.TestSupport.Auth;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;
using Tailbook.Api.Tests.TestSupport.Scenarios;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class PricingDurationFlowTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Admin_can_create_rule_sets_publish_and_preview_quote_with_persisted_snapshots()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var pet = await PetScenario.For(admin).CreateSchedulablePetAsync("Quote Client", "Quote preview pet");
        var catalogApi = CatalogScenario.For(admin);

        var offer = await catalogApi.CreateOfferAsync(CatalogScenario.UniqueCode("FULL"), "Package", "Full Groom Quote");
        var procedure = await catalogApi.CreateProcedureAsync(CatalogScenario.UniqueCode("FULLP"), "Full Groom Quote Procedure");
        var version = await catalogApi.CreateVersionAsync(offer.Id);
        await catalogApi.AddComponentAsync(version.Id, procedure.Id);
        await catalogApi.PublishOfferVersionAsync(version.Id);

        var priceRuleSet = await catalogApi.CreatePriceRuleSetAsync();
        await catalogApi.AddPriceRuleAsync(priceRuleSet.Id, offer.Id, 100, 1400m, pet.Catalog.SamoyedBreedId, null);
        await catalogApi.PublishPriceRuleSetAsync(priceRuleSet.Id);

        var durationRuleSet = await catalogApi.CreateDurationRuleSetAsync();
        await catalogApi.AddDurationRuleAsync(durationRuleSet.Id, offer.Id, 100, 120, 10, 15, pet.Catalog.SamoyedBreedId, null);
        await catalogApi.PublishDurationRuleSetAsync(durationRuleSet.Id);

        var previewResponse = await admin.PostAsJsonAsync("/api/admin/quotes/preview", new
        {
            petId = pet.PetId,
            items = new[]
            {
                new { itemType = "Package", offerId = offer.Id }
            }
        });

        previewResponse.ShouldBeOk();
        var preview = await previewResponse.ReadRequiredJsonAsync<PreviewQuoteEnvelope>();
        Assert.Equal(1400m, preview.PriceSnapshot.TotalAmount);
        Assert.Equal(120, preview.DurationSnapshot.ServiceMinutes);
        Assert.Equal(145, preview.DurationSnapshot.ReservedMinutes);
        Assert.NotEqual(Guid.Empty, preview.PriceSnapshot.Id);
        Assert.NotEqual(Guid.Empty, preview.DurationSnapshot.Id);
        Assert.Single(preview.PriceSnapshot.Lines);
        Assert.Equal(3, preview.DurationSnapshot.Lines.Length);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(dbContext.Set<PriceSnapshot>().Any(x => x.Id == preview.PriceSnapshot.Id));
        Assert.True(dbContext.Set<DurationSnapshot>().Any(x => x.Id == preview.DurationSnapshot.Id));
    }

    [Fact]
    public async Task More_specific_breed_price_rule_wins_over_animal_type_fallback()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var pet = await PetScenario.For(admin).CreateSchedulablePetAsync("Specificity Client", "Quote preview pet");
        var catalogApi = CatalogScenario.For(admin);

        var offer = await catalogApi.CreateOfferAsync(CatalogScenario.UniqueCode("BREED"), "Package", "Breed Rule Package");
        var procedure = await catalogApi.CreateProcedureAsync(CatalogScenario.UniqueCode("BREEDP"), "Breed Rule Package Procedure");
        var version = await catalogApi.CreateVersionAsync(offer.Id);
        await catalogApi.AddComponentAsync(version.Id, procedure.Id);
        await catalogApi.PublishOfferVersionAsync(version.Id);

        var priceRuleSet = await catalogApi.CreatePriceRuleSetAsync();
        await catalogApi.AddPriceRuleAsync(priceRuleSet.Id, offer.Id, 200, 1000m, null, pet.Catalog.DogAnimalTypeId);
        await catalogApi.AddPriceRuleAsync(priceRuleSet.Id, offer.Id, 100, 1500m, pet.Catalog.SamoyedBreedId, null);
        await catalogApi.PublishPriceRuleSetAsync(priceRuleSet.Id);

        var durationRuleSet = await catalogApi.CreateDurationRuleSetAsync();
        await catalogApi.AddDurationRuleAsync(durationRuleSet.Id, offer.Id, 100, 90, 5, 10, null, pet.Catalog.DogAnimalTypeId);
        await catalogApi.PublishDurationRuleSetAsync(durationRuleSet.Id);

        var previewResponse = await admin.PostAsJsonAsync("/api/admin/quotes/preview", new
        {
            petId = pet.PetId,
            items = new[]
            {
                new { itemType = "Package", offerId = offer.Id }
            }
        });

        previewResponse.ShouldBeOk();
        var preview = await previewResponse.ReadRequiredJsonAsync<PreviewQuoteEnvelope>();
        Assert.Equal(1500m, preview.PriceSnapshot.TotalAmount);
        Assert.Contains(preview.PriceSnapshot.Lines, x => x.Label.Contains("Samoyed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Groomer_cannot_access_pricing_or_quote_preview_endpoints()
    {
        using var client = await factory.CreateClientForRoleAsync(
            "groomer-pricing@test.local",
            "Groomer Pricing",
            TestUsers.GroomerPassword,
            "groomer");

        var pricingResponse = await client.GetAsync("/api/admin/pricing/rule-sets");
        pricingResponse.ShouldBeForbidden();

        var previewResponse = await client.PostAsJsonAsync("/api/admin/quotes/preview", new
        {
            petId = Guid.NewGuid(),
            items = new[]
            {
                new { itemType = "Package", offerId = Guid.NewGuid() }
            }
        });
        previewResponse.ShouldBeForbidden();
    }

    [Fact]
    public async Task Admin_can_list_price_and_duration_rule_sets_in_items_envelopes()
    {
        using var admin = await factory.CreateAdminClientAsync();
        var catalogApi = CatalogScenario.For(admin);

        var priceRuleSet = await catalogApi.CreatePriceRuleSetAsync();
        var durationRuleSet = await catalogApi.CreateDurationRuleSetAsync();

        var pricingResponse = await admin.GetAsync("/api/admin/pricing/rule-sets");
        pricingResponse.ShouldBeOk();
        var pricingPayload = await pricingResponse.ReadRequiredJsonAsync<PriceRuleSetsEnvelope>();
        Assert.Contains(pricingPayload.Items, x => x.Id == priceRuleSet.Id);

        var durationResponse = await admin.GetAsync("/api/admin/duration/rule-sets");
        durationResponse.ShouldBeOk();
        var durationPayload = await durationResponse.ReadRequiredJsonAsync<DurationRuleSetsEnvelope>();
        Assert.Contains(durationPayload.Items, x => x.Id == durationRuleSet.Id);
    }

    private sealed class PriceRuleSetsEnvelope
    {
        public RuleSetListItem[] Items { get; set; } = [];
    }

    private sealed class DurationRuleSetsEnvelope
    {
        public RuleSetListItem[] Items { get; set; } = [];
    }

    private sealed class RuleSetListItem
    {
        public Guid Id { get; set; }
    }
}
