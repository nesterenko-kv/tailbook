using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class GroomerPortalFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GroomerPortalFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Assigned_groomer_gets_privacy_safe_appointment_detail_without_contact_data()
    {
        var adminToken = await _factory.LoginAsAsync("admin@test.local", "Admin12345!");
        using var adminClient = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(adminClient, adminToken);

        var catalog = await GetPetCatalogAsync(adminClient);
        var clientId = await CreateClientAsync(adminClient, "Privacy Client");
        var contactId = await AddContactAsync(adminClient, clientId, "Olena", "Hidden");
        await AddContactMethodAsync(adminClient, contactId, "Phone", "+380991112233", "+380991112233");
        await AddContactMethodAsync(adminClient, contactId, "Instagram", "@privacy_hidden_owner", "@privacy_hidden_owner");

        var petId = await RegisterPetAsync(adminClient, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode, "Sensitive paws");
        var offerSetup = await CreateVisitReadyOfferAsync(adminClient, catalog.SamoyedBreedId);

        var groomerUserId = await _factory.SeedUserAsync("groomer.privacy@test.local", "Assigned Groomer", "Groomer123!", "groomer");
        var groomer = await CreateSchedulableGroomerAsync(adminClient, "Assigned Groomer", groomerUserId);
        var appointment = await CreateAppointmentAsync(adminClient, petId, groomer.Id, offerSetup.OfferId);

        var groomerToken = await _factory.LoginAsAsync("groomer.privacy@test.local", "Groomer123!");
        using var groomerClient = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(groomerClient, groomerToken);

        var response = await groomerClient.GetAsync($"/api/groomer/appointments/{appointment.Id:D}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sensitive paws", raw);
        Assert.DoesNotContain("+380991112233", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@privacy_hidden_owner", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientId", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contact", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Groomer_can_execute_own_visit_but_cannot_read_other_groomer_appointment()
    {
        var adminToken = await _factory.LoginAsAsync("admin@test.local", "Admin12345!");
        using var adminClient = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(adminClient, adminToken);

        var catalog = await GetPetCatalogAsync(adminClient);
        var clientId = await CreateClientAsync(adminClient, "Execution Client");
        var petId = await RegisterPetAsync(adminClient, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode, "Prefers calm handling");
        var offerSetup = await CreateVisitReadyOfferAsync(adminClient, catalog.SamoyedBreedId);

        var assignedUserId = await _factory.SeedUserAsync("groomer.exec@test.local", "Execution Groomer", "Groomer123!", "groomer");
        var otherUserId = await _factory.SeedUserAsync("groomer.other@test.local", "Other Groomer", "Groomer123!", "groomer");
        var assignedGroomer = await CreateSchedulableGroomerAsync(adminClient, "Execution Groomer", assignedUserId);
        await CreateSchedulableGroomerAsync(adminClient, "Other Groomer", otherUserId);

        var appointment = await CreateAppointmentAsync(adminClient, petId, assignedGroomer.Id, offerSetup.OfferId);

        var otherToken = await _factory.LoginAsAsync("groomer.other@test.local", "Groomer123!");
        using var otherClient = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(otherClient, otherToken);
        var forbiddenResponse = await otherClient.GetAsync($"/api/groomer/appointments/{appointment.Id:D}");
        Assert.Equal(HttpStatusCode.NotFound, forbiddenResponse.StatusCode);

        var assignedToken = await _factory.LoginAsAsync("groomer.exec@test.local", "Groomer123!");
        using var groomerClient = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(groomerClient, assignedToken);

        var checkInResponse = await groomerClient.PostAsJsonAsync($"/api/groomer/appointments/{appointment.Id:D}/check-in", new { appointmentId = appointment.Id });
        Assert.Equal(HttpStatusCode.Created, checkInResponse.StatusCode);
        var visit = await checkInResponse.Content.ReadFromJsonAsync<GroomerVisitEnvelope>();
        Assert.NotNull(visit);
        Assert.Equal("Open", visit!.Status);

        var executionItem = visit.Items.Single();
        var expectedComponent = executionItem.ExpectedComponents.First();

        var performedResponse = await groomerClient.PostAsJsonAsync($"/api/groomer/visits/{visit.Id:D}/performed-procedures", new
        {
            visitId = visit.Id,
            visitExecutionItemId = executionItem.Id,
            procedureId = expectedComponent.ProcedureId,
            note = "Completed by assigned groomer."
        });
        Assert.Equal(HttpStatusCode.OK, performedResponse.StatusCode);
        visit = await performedResponse.Content.ReadFromJsonAsync<GroomerVisitEnvelope>();
        Assert.NotNull(visit);
        Assert.Equal("InProgress", visit!.Status);
        Assert.NotEmpty(visit.Items.Single().PerformedProcedures);

        var skippedResponse = await groomerClient.PostAsJsonAsync($"/api/groomer/visits/{visit.Id:D}/skipped-components", new
        {
            visitId = visit.Id,
            visitExecutionItemId = executionItem.Id,
            offerVersionComponentId = expectedComponent.Id,
            omissionReasonCode = "OPERATIONAL_DECISION",
            note = "Skipped after execution test."
        });
        Assert.Equal(HttpStatusCode.OK, skippedResponse.StatusCode);
        visit = await skippedResponse.Content.ReadFromJsonAsync<GroomerVisitEnvelope>();
        Assert.NotNull(visit);
        Assert.NotEmpty(visit!.Items.Single().SkippedComponents);

        var currentVisitResponse = await groomerClient.GetAsync($"/api/groomer/appointments/{appointment.Id:D}/visit");
        Assert.Equal(HttpStatusCode.OK, currentVisitResponse.StatusCode);
    }

    private static async Task<Guid> CreateClientAsync(HttpClient client, string displayName)
    {
        var response = await client.PostAsJsonAsync("/api/admin/clients", new { displayName });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ClientEnvelope>();
        return payload!.Id;
    }

    private static async Task<Guid> AddContactAsync(HttpClient client, Guid clientId, string firstName, string lastName)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/clients/{clientId:D}/contacts", new
        {
            clientId,
            firstName,
            lastName
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ContactEnvelope>();
        return payload!.Id;
    }

    private static async Task AddContactMethodAsync(HttpClient client, Guid contactId, string methodType, string value, string displayValue)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/contacts/{contactId:D}/methods", new
        {
            contactId,
            methodType,
            value,
            displayValue,
            isPreferred = true
        });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<PetCatalogEnvelope> GetPetCatalogAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/admin/pets/catalog");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PetCatalogEnvelope>())!;
    }

    private static async Task<Guid> RegisterPetAsync(HttpClient client, Guid clientId, Guid breedId, string animalTypeCode, string coatTypeCode, string sizeCategoryCode, string notes)
    {
        var response = await client.PostAsJsonAsync("/api/admin/pets", new
        {
            clientId,
            name = "Milo",
            animalTypeCode,
            breedId,
            coatTypeCode,
            sizeCategoryCode,
            notes
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<PetEnvelope>();
        return payload!.Id;
    }

    private static async Task<(Guid OfferId, Guid SecondProcedureId)> CreateVisitReadyOfferAsync(HttpClient client, Guid breedId)
    {
        var offerResponse = await client.PostAsJsonAsync("/api/admin/catalog/offers", new { code = $"V8_{Guid.NewGuid():N}"[..14], offerType = "Package", displayName = "Groomer Visit Package" });
        offerResponse.EnsureSuccessStatusCode();
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferEnvelope>();

        var procedureOneResponse = await client.PostAsJsonAsync("/api/admin/catalog/procedures", new { code = $"GVP_{Guid.NewGuid():N}"[..14], name = "Bathing" });
        procedureOneResponse.EnsureSuccessStatusCode();
        var procedureOne = await procedureOneResponse.Content.ReadFromJsonAsync<ProcedureEnvelope>();

        var procedureTwoResponse = await client.PostAsJsonAsync("/api/admin/catalog/procedures", new { code = $"GVP_{Guid.NewGuid():N}"[..14], name = "Drying" });
        procedureTwoResponse.EnsureSuccessStatusCode();
        var procedureTwo = await procedureTwoResponse.Content.ReadFromJsonAsync<ProcedureEnvelope>();

        var versionResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offers/{offer!.Id:D}/versions", new { offerId = offer.Id });
        versionResponse.EnsureSuccessStatusCode();
        var version = await versionResponse.Content.ReadFromJsonAsync<OfferVersionEnvelope>();

        foreach (var tuple in new[] { (procedureOne!.Id, 1), (procedureTwo!.Id, 2) })
        {
            var componentResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version!.Id:D}/components", new
            {
                versionId = version.Id,
                procedureId = tuple.Id,
                componentRole = "Included",
                sequenceNo = tuple.Item2,
                defaultExpected = true
            });
            componentResponse.EnsureSuccessStatusCode();
        }

        var publishVersionResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version!.Id:D}/publish", new { versionId = version.Id });
        publishVersionResponse.EnsureSuccessStatusCode();

        var priceRuleSet = await client.PostAsJsonAsync("/api/admin/pricing/rule-sets", new { });
        priceRuleSet.EnsureSuccessStatusCode();
        var priceRuleSetPayload = await priceRuleSet.Content.ReadFromJsonAsync<RuleSetEnvelope>();
        var priceRuleResponse = await client.PostAsJsonAsync($"/api/admin/pricing/rule-sets/{priceRuleSetPayload!.Id:D}/rules", new
        {
            ruleSetId = priceRuleSetPayload.Id,
            offerId = offer.Id,
            priority = 100,
            fixedAmount = 1400,
            currency = "UAH",
            breedId
        });
        priceRuleResponse.EnsureSuccessStatusCode();
        var publishPriceResponse = await client.PostAsJsonAsync($"/api/admin/pricing/rule-sets/{priceRuleSetPayload.Id:D}/publish", new { ruleSetId = priceRuleSetPayload.Id });
        publishPriceResponse.EnsureSuccessStatusCode();

        var durationRuleSet = await client.PostAsJsonAsync("/api/admin/duration/rule-sets", new { });
        durationRuleSet.EnsureSuccessStatusCode();
        var durationRuleSetPayload = await durationRuleSet.Content.ReadFromJsonAsync<RuleSetEnvelope>();
        var durationRuleResponse = await client.PostAsJsonAsync($"/api/admin/duration/rule-sets/{durationRuleSetPayload!.Id:D}/rules", new
        {
            ruleSetId = durationRuleSetPayload.Id,
            offerId = offer.Id,
            priority = 100,
            baseMinutes = 110,
            bufferBeforeMinutes = 5,
            bufferAfterMinutes = 10,
            breedId
        });
        durationRuleResponse.EnsureSuccessStatusCode();
        var publishDurationResponse = await client.PostAsJsonAsync($"/api/admin/duration/rule-sets/{durationRuleSetPayload.Id:D}/publish", new { ruleSetId = durationRuleSetPayload.Id });
        publishDurationResponse.EnsureSuccessStatusCode();

        return (offer.Id, procedureTwo.Id);
    }

    private static async Task<GroomerEnvelope> CreateSchedulableGroomerAsync(HttpClient client, string displayName, Guid userId)
    {
        var createResponse = await client.PostAsJsonAsync("/api/admin/groomers", new { displayName, userId });
        createResponse.EnsureSuccessStatusCode();
        var groomer = await createResponse.Content.ReadFromJsonAsync<GroomerEnvelope>();

        var scheduleResponse = await client.PostAsJsonAsync($"/api/admin/groomers/{groomer!.Id:D}/working-schedules", new
        {
            groomerId = groomer.Id,
            weekday = 5,
            startLocalTime = "09:00",
            endLocalTime = "18:00"
        });
        scheduleResponse.EnsureSuccessStatusCode();

        return groomer;
    }

    private static async Task<AppointmentEnvelope> CreateAppointmentAsync(HttpClient client, Guid petId, Guid groomerId, Guid offerId)
    {
        var response = await client.PostAsJsonAsync("/api/admin/appointments", new
        {
            petId,
            groomerId,
            startAtUtc = DateTime.Parse("2026-04-24T07:00:00Z").ToUniversalTime(),
            items = new[] { new { offerId, itemType = "Package" } }
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AppointmentEnvelope>())!;
    }

    private sealed class ClientEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class ContactEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class PetEnvelope
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

    private sealed class GroomerEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class AppointmentEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class GroomerVisitEnvelope
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public GroomerVisitItemEnvelope[] Items { get; set; } = [];
    }

    private sealed class GroomerVisitItemEnvelope
    {
        public Guid Id { get; set; }
        public GroomerVisitExpectedComponentEnvelope[] ExpectedComponents { get; set; } = [];
        public GroomerVisitPerformedProcedureEnvelope[] PerformedProcedures { get; set; } = [];
        public GroomerVisitSkippedComponentEnvelope[] SkippedComponents { get; set; } = [];
    }

    private sealed class GroomerVisitExpectedComponentEnvelope
    {
        public Guid Id { get; set; }
        public Guid ProcedureId { get; set; }
    }

    private sealed class GroomerVisitPerformedProcedureEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class GroomerVisitSkippedComponentEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class PetCatalogEnvelope
    {
        public AnimalTypeEnvelope[] AnimalTypes { get; set; } = [];
        public BreedEnvelope[] Breeds { get; set; } = [];
        public CoatTypeEnvelope[] CoatTypes { get; set; } = [];
        public SizeCategoryEnvelope[] SizeCategories { get; set; } = [];

        public string DogAnimalTypeCode => AnimalTypes.Single(x => x.Code == "DOG").Code;
        public Guid SamoyedBreedId => Breeds.Single(x => x.Code == "SAMOYED").Id;
        public string DoubleCoatCode => CoatTypes.Single(x => x.Code == "DOUBLE_COAT").Code;
        public string LargeSizeCode => SizeCategories.Single(x => x.Code == "LARGE").Code;
    }

    private sealed class AnimalTypeEnvelope
    {
        public string Code { get; set; } = string.Empty;
    }

    private sealed class BreedEnvelope
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    private sealed class CoatTypeEnvelope
    {
        public string Code { get; set; } = string.Empty;
    }

    private sealed class SizeCategoryEnvelope
    {
        public string Code { get; set; } = string.Empty;
    }
}
