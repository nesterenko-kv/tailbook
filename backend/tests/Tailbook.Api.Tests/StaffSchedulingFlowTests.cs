using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class StaffSchedulingFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StaffSchedulingFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_can_create_groomer_configure_schedule_and_detect_blocked_availability()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var offer = await CreateOfferAsync(client, "SCHEDULE_CHECK", "StandaloneService", "Schedule Check");
        var groomer = await CreateGroomerAsync(client, "Iryna");

        var scheduleResponse = await client.PostAsJsonAsync($"/api/admin/groomers/{groomer.Id:D}/working-schedules", new
        {
            groomerId = groomer.Id,
            weekday = 1,
            startLocalTime = "09:00",
            endLocalTime = "18:00"
        });
        Assert.Equal(HttpStatusCode.Created, scheduleResponse.StatusCode);

        var catalog = await GetPetCatalogAsync(client);
        var clientId = await CreateClientAsync(client, "Schedule Client");
        var petId = await RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);

        var availableResponse = await client.PostAsJsonAsync($"/api/admin/groomers/{groomer.Id:D}/availability/check", new
        {
            groomerId = groomer.Id,
            petId,
            startAtUtc = DateTime.Parse("2026-04-20T07:00:00Z").ToUniversalTime(),
            reservedMinutes = 90,
            offerIds = new[] { offer.Id }
        });
        Assert.Equal(HttpStatusCode.OK, availableResponse.StatusCode);
        var available = await availableResponse.Content.ReadFromJsonAsync<AvailabilityEnvelope>();
        Assert.NotNull(available);
        Assert.True(available!.IsAvailable);
        Assert.Equal(DateTime.Parse("2026-04-20T08:30:00Z").ToUniversalTime(), available.EndAtUtc);

        var blockResponse = await client.PostAsJsonAsync($"/api/admin/groomers/{groomer.Id:D}/time-blocks", new
        {
            groomerId = groomer.Id,
            startAtUtc = DateTime.Parse("2026-04-20T07:30:00Z").ToUniversalTime(),
            endAtUtc = DateTime.Parse("2026-04-20T08:15:00Z").ToUniversalTime(),
            reasonCode = "LUNCH",
            notes = "Lunch overlap"
        });
        Assert.Equal(HttpStatusCode.Created, blockResponse.StatusCode);

        var blockedResponse = await client.PostAsJsonAsync($"/api/admin/groomers/{groomer.Id:D}/availability/check", new
        {
            groomerId = groomer.Id,
            petId,
            startAtUtc = DateTime.Parse("2026-04-20T07:00:00Z").ToUniversalTime(),
            reservedMinutes = 90,
            offerIds = new[] { offer.Id }
        });
        Assert.Equal(HttpStatusCode.OK, blockedResponse.StatusCode);
        var blocked = await blockedResponse.Content.ReadFromJsonAsync<AvailabilityEnvelope>();
        Assert.NotNull(blocked);
        Assert.False(blocked!.IsAvailable);
        Assert.Contains(blocked.Reasons, x => x.Contains("blocked time", StringComparison.OrdinalIgnoreCase));

        var scheduleReadResponse = await client.GetAsync($"/api/admin/groomers/{groomer.Id:D}/schedule?fromUtc=2026-04-20T00:00:00Z&toUtc=2026-04-21T00:00:00Z");
        Assert.Equal(HttpStatusCode.OK, scheduleReadResponse.StatusCode);
        var schedule = await scheduleReadResponse.Content.ReadFromJsonAsync<GroomerScheduleEnvelope>();
        Assert.NotNull(schedule);
        Assert.Single(schedule!.TimeBlocks);
        Assert.NotEmpty(schedule.AvailabilityWindows);
    }

    [Fact]
    public async Task Groomer_capability_modifier_is_applied_to_quote_preview_reserved_duration()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var catalog = await GetPetCatalogAsync(client);
        var clientId = await CreateClientAsync(client, "Modifier Client");
        var petId = await RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);

        var offer = await CreateOfferAsync(client, "MODIFIER_PACKAGE", "Package", "Modifier Package");
        var procedure = await CreateProcedureAsync(client, "MODIFIER_PROC", "Modifier Procedure");
        var version = await CreateVersionAsync(client, offer.Id);
        await AddComponentAsync(client, version.Id, procedure.Id);
        await PublishOfferVersionAsync(client, version.Id);

        var priceRuleSet = await CreatePriceRuleSetAsync(client);
        await AddPriceRuleAsync(client, priceRuleSet.Id, offer.Id, 100, 1200m, catalog.SamoyedBreedId, null);
        await PublishPriceRuleSetAsync(client, priceRuleSet.Id);

        var durationRuleSet = await CreateDurationRuleSetAsync(client);
        await AddDurationRuleAsync(client, durationRuleSet.Id, offer.Id, 100, 90, 5, 10, catalog.SamoyedBreedId, null);
        await PublishDurationRuleSetAsync(client, durationRuleSet.Id);

        var groomer = await CreateGroomerAsync(client, "Oksana");
        await client.PostAsJsonAsync($"/api/admin/groomers/{groomer.Id:D}/capabilities", new
        {
            groomerId = groomer.Id,
            breedId = catalog.SamoyedBreedId,
            offerId = offer.Id,
            capabilityMode = "Allow",
            reservedDurationModifierMinutes = 15
        });

        var previewResponse = await client.PostAsJsonAsync("/api/admin/quotes/preview", new
        {
            petId,
            groomerId = groomer.Id,
            items = new[]
            {
                new { itemType = "Package", offerId = offer.Id }
            }
        });

        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var preview = await previewResponse.Content.ReadFromJsonAsync<PreviewQuoteEnvelope>();
        Assert.NotNull(preview);
        Assert.Equal(120, preview!.DurationSnapshot.ReservedMinutes);
        Assert.Contains(preview.DurationSnapshot.Lines, x => x.LineType == "GroomerCapabilityModifier");
    }

    [Fact]
    public async Task Deny_capability_blocks_availability_check_with_bad_request()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var catalog = await GetPetCatalogAsync(client);
        var clientId = await CreateClientAsync(client, "Blocked Client");
        var petId = await RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);

        var offer = await CreateOfferAsync(client, "DENY_CHECK", "StandaloneService", "Deny Check");
        var groomer = await CreateGroomerAsync(client, "Denied Groomer");
        await client.PostAsJsonAsync($"/api/admin/groomers/{groomer.Id:D}/working-schedules", new
        {
            groomerId = groomer.Id,
            weekday = 1,
            startLocalTime = "09:00",
            endLocalTime = "18:00"
        });

        var denyResponse = await client.PostAsJsonAsync($"/api/admin/groomers/{groomer.Id:D}/capabilities", new
        {
            groomerId = groomer.Id,
            breedId = catalog.SamoyedBreedId,
            offerId = offer.Id,
            capabilityMode = "Deny",
            reservedDurationModifierMinutes = 0
        });
        Assert.Equal(HttpStatusCode.Created, denyResponse.StatusCode);

        var availabilityResponse = await client.PostAsJsonAsync($"/api/admin/groomers/{groomer.Id:D}/availability/check", new
        {
            groomerId = groomer.Id,
            petId,
            startAtUtc = DateTime.Parse("2026-04-20T07:00:00Z").ToUniversalTime(),
            reservedMinutes = 90,
            offerIds = new[] { offer.Id }
        });

        Assert.Equal(HttpStatusCode.BadRequest, availabilityResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_can_list_groomers_in_items_envelope()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var created = await CreateGroomerAsync(client, "Envelope Groomer");

        var response = await client.GetAsync("/api/admin/groomers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ListGroomersEnvelope>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload!.Items);
        Assert.Contains(payload.Items, x => x.Id == created.Id);
    }

    private static async Task<PetCatalogEnvelope> GetPetCatalogAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/admin/pets/catalog");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PetCatalogEnvelope>())!;
    }

    private static async Task<Guid> CreateClientAsync(HttpClient client, string displayName)
    {
        var response = await client.PostAsJsonAsync("/api/admin/clients", new { displayName });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateClientEnvelope>())!.Id;
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
            notes = "Staff scheduling pet"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PetEnvelope>())!.Id;
    }

    private static async Task<GroomerEnvelope> CreateGroomerAsync(HttpClient client, string displayName)
    {
        var response = await client.PostAsJsonAsync("/api/admin/groomers", new { displayName });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GroomerEnvelope>())!;
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

    private static async Task AddDurationRuleAsync(HttpClient client, Guid ruleSetId, Guid offerId, int priority, int baseMinutes, int bufferBeforeMinutes, int bufferAfterMinutes, Guid? breedId, Guid? animalTypeId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/duration/rule-sets/{ruleSetId:D}/rules", new
        {
            ruleSetId,
            offerId,
            priority,
            baseMinutes,
            bufferBeforeMinutes,
            bufferAfterMinutes,
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

    private sealed class AvailabilityEnvelope
    {
        public bool IsAvailable { get; set; }
        public DateTime EndAtUtc { get; set; }
        public int CheckedReservedMinutes { get; set; }
        public string[] Reasons { get; set; } = [];
    }

    private sealed class GroomerScheduleEnvelope
    {
        public TimeBlockEnvelope[] TimeBlocks { get; set; } = [];
        public AvailabilityWindowEnvelope[] AvailabilityWindows { get; set; } = [];
    }

    private sealed class TimeBlockEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class AvailabilityWindowEnvelope
    {
        public DateTime StartAtUtc { get; set; }
        public DateTime EndAtUtc { get; set; }
    }

    private sealed class PreviewQuoteEnvelope
    {
        public DurationSnapshotEnvelope DurationSnapshot { get; set; } = new();
    }

    private sealed class DurationSnapshotEnvelope
    {
        public int ReservedMinutes { get; set; }
        public DurationLineEnvelope[] Lines { get; set; } = [];
    }

    private sealed class DurationLineEnvelope
    {
        public string LineType { get; set; } = string.Empty;
    }

    private sealed class PetCatalogEnvelope
    {
        public CatalogAnimalType[] AnimalTypes { get; set; } = [];
        public CatalogBreed[] Breeds { get; set; } = [];
        public CatalogCoatType[] CoatTypes { get; set; } = [];
        public CatalogSizeCategory[] SizeCategories { get; set; } = [];

        public Guid SamoyedBreedId => Breeds.Single(x => x.Code == "SAMOYED").Id;
        public Guid DogAnimalTypeId => AnimalTypes.Single(x => x.Code == "DOG").Id;
        public string DogAnimalTypeCode => AnimalTypes.Single(x => x.Code == "DOG").Code;
        public string DoubleCoatCode => CoatTypes.Single(x => x.Code == "DOUBLE_COAT").Code;
        public string LargeSizeCode => SizeCategories.Single(x => x.Code == "LARGE").Code;
    }

    private sealed class CatalogAnimalType
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    private sealed class CatalogBreed
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    private sealed class CatalogCoatType
    {
        public string Code { get; set; } = string.Empty;
    }

    private sealed class CatalogSizeCategory
    {
        public string Code { get; set; } = string.Empty;
    }

    private sealed class CreateClientEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class PetEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class GroomerEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class OfferEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class ProcedureEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class OfferVersionEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class RuleSetEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class ListGroomersEnvelope
    {
        public GroomerListItemEnvelope[] Items { get; set; } = [];
    }

    private sealed class GroomerListItemEnvelope
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }
}
